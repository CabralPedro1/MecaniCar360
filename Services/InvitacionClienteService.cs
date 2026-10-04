using System.Data;
using System.Net;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class InvitacionClienteService(MecaniCarContext db, PermisoService permisos,
    AuditoriaService auditoria, EmailService correo, IConfiguration config,
    IWebHostEnvironment ambiente, ILogger<InvitacionClienteService> logger,
    ClienteHabilitadoService habilitacion)
{
    public const string InvitacionNoDisponible = "La invitacion no esta disponible. Solicite una nueva al taller.";

    private Task<bool> EsClienteAsync(int id) => db.PersonaRoles.AsNoTracking().AnyAsync(pr =>
        pr.PersonaId == id && pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.CLIENTE);
    private async Task<bool> PuedeEmitirAsync(int actor) =>
        await permisos.TienePermisoAsync(actor, "USUARIO_CREAR") &&
        await permisos.TienePermisoAsync(actor, "PERSONA_VER");
    private bool ContextoDisponible() => db.Database.CurrentTransaction == null && !db.ChangeTracker.HasChanges();

    // Orden comun: Persona (UPDLOCK,HOLDLOCK) -> rol/Usuario -> invitaciones.
    // Toda escritura de invitaciones para una Persona comparte este mutex, incluso reenvio y fallo SMTP.
    private Task<Persona?> BloquearPersonaAsync(int id) => db.Personas.FromSqlInterpolated(
        $"SELECT * FROM [Personas] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = {id}")
        .AsNoTracking().SingleOrDefaultAsync();

    private async Task<bool> ElegibleAsync(Persona? persona) => persona?.Activo == true &&
        IdentificadorCuenta.EmailValido(persona.Email) && await EsClienteAsync(persona.Id) &&
        !await db.Usuarios.AnyAsync(u => u.PersonaId == persona.Id);

    internal static bool Vigente(InvitacionCliente i, Persona p, DateTime ahora) =>
        i.FechaConsumida == null && i.FechaInvalidacion == null && ahora < i.FechaExpiracion &&
        string.Equals(IdentificadorCuenta.Normalizar(p.Email), i.EmailDestino, StringComparison.OrdinalIgnoreCase);

    private Task<int> InvalidarAsync(int personaId, DateTime ahora) => db.InvitacionesCliente
        .Where(i => i.PersonaId == personaId && i.FechaConsumida == null && i.FechaInvalidacion == null)
        .ExecuteUpdateAsync(s => s.SetProperty(i => i.FechaInvalidacion, ahora));

    public async Task<ServiceResult<EstadoCuentaCliente>> ObtenerEstadoAsync(int personaId, int actor)
    {
        if (!await permisos.TienePermisoAsync(actor, "PERSONA_VER"))
            return ServiceResult<EstadoCuentaCliente>.Error("Acceso denegado.");
        var p = await db.Personas.AsNoTracking().SingleOrDefaultAsync(p => p.Id == personaId);
        if (p == null) return ServiceResult<EstadoCuentaCliente>.Error("Persona no disponible.");
        var tiene = await db.Usuarios.AnyAsync(u => u.PersonaId == personaId);
        var elegible = await ElegibleAsync(p);
        var pendientes = await db.InvitacionesCliente.AsNoTracking().Where(i => i.PersonaId == personaId &&
            i.FechaConsumida == null && i.FechaInvalidacion == null && i.FechaExpiracion > DateTime.UtcNow).ToListAsync();
        return ServiceResult<EstadoCuentaCliente>.Ok(new(tiene, await habilitacion.EstaHabilitadoAsync(personaId),
            elegible && await PuedeEmitirAsync(actor), elegible && pendientes.Any(i => Vigente(i, p, DateTime.UtcNow))));
    }

    private Uri UrlBase()
    {
        if (!Uri.TryCreate(config["Invitaciones:UrlBase"], UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            !(uri.Scheme == "https" || (ambiente.IsDevelopment() && uri.Scheme == "http" && uri.IsLoopback)))
            throw new InvalidOperationException("Configure Invitaciones:UrlBase con HTTPS (HTTP localhost solo en Development).");
        return new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
    }

    public async Task<ServiceResult> EmitirAsync(int personaId, int actor)
    {
        if (!await PuedeEmitirAsync(actor)) return ServiceResult.Error("Acceso denegado.");
        if (!ContextoDisponible()) return ServiceResult.Error("Hay otra operacion pendiente.");
        Uri baseUri;
        try { baseUri = UrlBase(); }
        catch (InvalidOperationException) { return ServiceResult.Error("Falta una URL publica de activacion valida en Invitaciones:UrlBase."); }
        var token = TokenInvitacion.Generar();
        InvitacionCliente nueva;
        var anteriores = db.ChangeTracker.Entries().Select(e => e.Entity).ToHashSet(ReferenceEqualityComparer.Instance);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var p = await BloquearPersonaAsync(personaId);
            if (!await ElegibleAsync(p)) return ServiceResult.Error("El cliente no es elegible para una invitacion.");
            if (!await PuedeEmitirAsync(actor)) return ServiceResult.Error("Acceso denegado.");
            var email = IdentificadorCuenta.Normalizar(p!.Email);
            if (await db.Usuarios.AnyAsync(u => u.EmailLogin == email))
                return ServiceResult.Error("No se puede emitir la invitacion con el email indicado.");
            var reemision = await db.InvitacionesCliente.AnyAsync(i => i.PersonaId == personaId);
            var ahora = DateTime.UtcNow;
            await InvalidarAsync(personaId, ahora);
            nueva = new InvitacionCliente { PersonaId = personaId, EmailDestino = email,
                TokenHash = TokenInvitacion.Hash(token), FechaCreacion = ahora, FechaExpiracion = ahora.AddHours(48),
                EmitidaPorUsuarioId = actor };
            db.InvitacionesCliente.Add(nueva);
            await db.SaveChangesAsync();
            auditoria.RegistrarOperacion(reemision ? "INVITACION_REEMITIDA" : "INVITACION_EMITIDA",
                "InvitacionCliente", nueva.Id, actor);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException or SqlException)
        {
            logger.LogWarning("No se pudo guardar una invitacion. Tipo: {Tipo}", ex.GetType().Name);
            return ServiceResult.Error("No se pudo emitir la invitacion. Reintente.");
        }
        finally { DesvincularNuevas(anteriores); }

        try
        {
            // El token viaja en el fragmento: nunca llega en URL/query a servidor o proxy.
            // La pagina lo retira del historial y lo envia por encabezado GET; el consumo usa POST con antiforgery.
            var enlace = new Uri(baseUri, "ActivacionCliente/Index").AbsoluteUri + "#" + token;
            await correo.EnviarCorreoAsync(nueva.EmailDestino, "Activar cuenta MecaniCar",
                "Recibio una invitacion para activar su cuenta de MecaniCar. Elija su usuario y contrasena. " +
                "La invitacion vence en 48 horas. <a href=\"" + WebUtility.HtmlEncode(enlace) + "\">Activar cuenta</a>");
            return ServiceResult.Ok("Invitacion enviada.");
        }
        catch (Exception ex)
        {
            // SMTP puede tener resultado ambiguo. Se revoca conservadoramente el token incluso si fue entregado.
            logger.LogWarning("No se confirmo el envio de una invitacion. Tipo: {Tipo}", ex.GetType().Name);
            try
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                await BloquearPersonaAsync(personaId);
                await db.InvitacionesCliente.Where(i => i.Id == nueva.Id && i.FechaConsumida == null && i.FechaInvalidacion == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(i => i.FechaInvalidacion, DateTime.UtcNow));
                auditoria.RegistrarOperacion("INVITACION_ENVIO_FALLIDO", "InvitacionCliente", nueva.Id, actor,
                    "No se confirmo el envio. Se solicito invalidar la invitacion.");
                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception fallo)
            {
                logger.LogError("No se pudo confirmar la invalidacion de una invitacion. Tipo: {Tipo}", fallo.GetType().Name);
                return ServiceResult.Error("No se confirmo el envio ni la invalidacion. Reemita la invitacion antes de continuar.");
            }
            finally { DesvincularNuevas(anteriores); }
            return ServiceResult.Error("No se pudo confirmar el envio. La invitacion fue invalidada; puede reenviarla.");
        }
    }

    public async Task<bool> ValidarAsync(string? token)
    {
        if (!TokenInvitacion.FormatoValido(token)) return false;
        var hash = TokenInvitacion.Hash(token!);
        var i = await db.InvitacionesCliente.AsNoTracking().SingleOrDefaultAsync(i => i.TokenHash == hash);
        if (i == null) return false;
        var p = await db.Personas.AsNoTracking().SingleOrDefaultAsync(p => p.Id == i.PersonaId);
        return await ElegibleAsync(p) && Vigente(i, p!, DateTime.UtcNow);
    }

    public async Task<ServiceResult> ActivarAsync(ActivarClienteViewModel vm)
    {
        if (!TokenInvitacion.FormatoValido(vm.Token)) return ServiceResult.Error(InvitacionNoDisponible);
        var username = IdentificadorCuenta.Normalizar(vm.Username);
        if (!IdentificadorCuenta.UsernameValido(username)) return ServiceResult.Error("Usuario obligatorio, hasta 50 caracteres y sin @.");
        if (!PasswordValidator.EsValida(vm.Password, out var error)) return ServiceResult.Error(error);
        if (vm.Password != vm.ConfirmarPassword) return ServiceResult.Error("Las contrasenas no coinciden.");
        if (!ContextoDisponible()) return ServiceResult.Error("Hay otra operacion pendiente.");
        var hash = TokenInvitacion.Hash(vm.Token);
        // Solo localiza el mutex. Todas las decisiones se repiten dentro de la transaccion.
        var personaId = await db.InvitacionesCliente.AsNoTracking().Where(i => i.TokenHash == hash)
            .Select(i => (int?)i.PersonaId).SingleOrDefaultAsync();
        if (!personaId.HasValue) return ServiceResult.Error(InvitacionNoDisponible);
        var anteriores = db.ChangeTracker.Entries().Select(e => e.Entity).ToHashSet(ReferenceEqualityComparer.Instance);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var p = await BloquearPersonaAsync(personaId.Value);
            if (!await ElegibleAsync(p)) return ServiceResult.Error(InvitacionNoDisponible);
            var i = await db.InvitacionesCliente.AsNoTracking().SingleOrDefaultAsync(i => i.TokenHash == hash);
            if (i == null || !Vigente(i, p!, DateTime.UtcNow)) return ServiceResult.Error(InvitacionNoDisponible);
            if (await db.Usuarios.AnyAsync(u => u.Username == username))
                return ServiceResult.Error("El nombre de usuario no esta disponible.");
            if (await db.Usuarios.AnyAsync(u => u.EmailLogin == i.EmailDestino))
                return ServiceResult.Error("No se puede activar la cuenta. Contacte al taller.");
            var usuario = new Usuario { PersonaId = p!.Id, Username = username, EmailLogin = i.EmailDestino,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password), Activo = true, PrimerLogin = false,
                SecurityStamp = Guid.NewGuid().ToString("N") };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
            var ahora = DateTime.UtcNow;
            await db.InvitacionesCliente.Where(x => x.Id == i.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.FechaConsumida, ahora));
            await InvalidarAsync(p.Id, ahora);
            auditoria.RegistrarActivacionCliente(usuario.Id);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Ok("Cuenta activada. Ya puede iniciar sesion con su usuario y contrasena.");
        }
        catch (Exception ex) when (EsVictimaDeadlock(ex))
        {
            // El await using ya dispuso la transaccion abortada; no continuar ni reintentar.
            logger.LogWarning("Activacion interrumpida por conflicto transitorio SQL 1205.");
            return ServiceResult.Error("No se pudo completar la activacion en este momento. Intente nuevamente.");
        }
        catch (Exception ex) when (ex is DbUpdateException or SqlException)
        {
            logger.LogWarning("No se pudo completar una activacion. Tipo: {Tipo}", ex.GetType().Name);
            return ServiceResult.Error("No se pudo activar la cuenta. Verifique la invitacion y la disponibilidad del usuario.");
        }
        finally { DesvincularNuevas(anteriores); }
    }

    private static bool EsVictimaDeadlock(Exception excepcion)
    {
        for (Exception? actual = excepcion; actual != null; actual = actual.InnerException)
            if (actual is SqlException { Number: 1205 }) return true;
        return false;
    }

    private void DesvincularNuevas(HashSet<object> anteriores)
    {
        foreach (var e in db.ChangeTracker.Entries().Where(e => !anteriores.Contains(e.Entity)).ToList())
            e.State = EntityState.Detached;
    }
}
