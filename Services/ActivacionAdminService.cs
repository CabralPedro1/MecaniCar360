using System.ComponentModel.DataAnnotations;
using System.Data;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class ActivacionAdminService(MecaniCarContext db, EmailService correo,
    IConfiguration config, TimeProvider reloj, ILogger<ActivacionAdminService> logger)
{
    public const string NoDisponible = "El enlace de activación no está disponible. Solicite asistencia al responsable de la instalación.";
    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;
    private IQueryable<Usuario> Usuarios() => db.Usuarios.Include(u => u.Persona)
        .ThenInclude(p => p.Roles).ThenInclude(r => r.Rol);
    private bool ContextoDisponible() => db.Database.CurrentTransaction == null && !db.ChangeTracker.Entries().Any();

    private async Task<Usuario?> BloquearAsync(int id)
    {
        var persona = await db.Usuarios.AsNoTracking().Where(u => u.Id == id)
            .Select(u => (int?)u.PersonaId).SingleOrDefaultAsync();
        if (!persona.HasValue) return null;
        // El interceptor de la transacción adquirió primero el bloqueo global de integridad.
        await db.Personas.FromSqlInterpolated(
            $"SELECT * FROM [Personas] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={persona.Value}").AsNoTracking().SingleAsync();
        return await Usuarios().SingleOrDefaultAsync(u => u.Id == id);
    }

    public async Task<bool> ValidarAsync(string? token)
    {
        if (!TokenInvitacion.FormatoValido(token)) return false;
        var hash = ActivacionAdminInicial.HashToken(token!);
        var enlace = await db.EnlacesPasswordCliente.AsNoTracking().SingleOrDefaultAsync(e => e.TokenHash == hash);
        if (enlace == null) return false;
        var u = await Usuarios().AsNoTracking().SingleOrDefaultAsync(u => u.Id == enlace.UsuarioId);
        return u != null && ActivacionAdminInicial.EsVigente(u, enlace, ActivacionAdminInicial.EmailConfigurado(config), Ahora);
    }

    public async Task<ServiceResult> ActivarAsync(ActivarAdminViewModel vm)
    {
        if (!TokenInvitacion.FormatoValido(vm.Token)) return ServiceResult.Error(NoDisponible);
        if (!Validator.TryValidateObject(vm, new ValidationContext(vm), new List<ValidationResult>(), true))
            return ServiceResult.Error("Revise los datos y la confirmación de contraseña.");
        if (!PasswordValidator.EsValida(vm.Password, out var error)) return ServiceResult.Error(error);
        if (!ContextoDisponible()) return ServiceResult.Error(NoDisponible);
        var email = ActivacionAdminInicial.EmailConfigurado(config);
        var hash = ActivacionAdminInicial.HashToken(vm.Token);
        var id = await db.EnlacesPasswordCliente.AsNoTracking().Where(e => e.TokenHash == hash)
            .Select(e => (int?)e.UsuarioId).SingleOrDefaultAsync();
        if (!id.HasValue) return ServiceResult.Error(NoDisponible);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var u = await BloquearAsync(id.Value);
            var enlace = await db.EnlacesPasswordCliente.SingleOrDefaultAsync(e => e.UsuarioId == id);
            if (u == null || enlace?.TokenHash != hash || !ActivacionAdminInicial.EsVigente(u, enlace, email, Ahora))
                return ServiceResult.Error(NoDisponible);
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password);
            u.SecurityStamp = Guid.NewGuid().ToString("N");
            u.Activo = true;
            u.PrimerLogin = false;
            u.Persona.Telefono = vm.Telefono;
            enlace.FechaConsumida = Ahora;
            AuditoriaService.RegistrarBootstrapAdmin(db, "ADMIN_ACTIVADO", u.Id);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Ok("Cuenta activada. Inicie sesión con su nueva contraseña.");
        }
        finally { db.ChangeTracker.Clear(); }
    }

    // No es un endpoint ni una operación disponible desde HTTP. La capacidad sólo
    // se obtiene comprobando el operador Windows y el destino desde el comando local.
    internal async Task<ServiceResult> EmitirLocalAsync(BootstrapAdminLocal.Autorizacion autorizacion)
    {
        ArgumentNullException.ThrowIfNull(autorizacion);
        autorizacion.ComprobarDestino(db);
        if (!ContextoDisponible()) return ServiceResult.Error(NoDisponible);
        var email = ActivacionAdminInicial.EmailConfigurado(config);
        if (!Uri.TryCreate(config["Invitaciones:UrlBase"], UriKind.Absolute, out var origen) ||
            origen.Scheme != "https" || !string.IsNullOrEmpty(origen.UserInfo) ||
            !string.IsNullOrEmpty(origen.Query) || !string.IsNullOrEmpty(origen.Fragment))
            return ServiceResult.Error("Configure Invitaciones:UrlBase con un origen HTTPS confiable.");
        var ids = await db.Usuarios.AsNoTracking().Where(u => u.EmailLogin == email)
            .Select(u => u.Id).Take(2).ToListAsync();
        if (ids.Count != 1) return ServiceResult.Error(NoDisponible);
        string token;
        string hash;
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var u = await BloquearAsync(ids[0]);
            var e = await db.EnlacesPasswordCliente.SingleOrDefaultAsync(e => e.UsuarioId == ids[0]);
            if (u == null || !ActivacionAdminInicial.EsPendiente(u, e, email)) return ServiceResult.Error(NoDisponible);
            var ahora = Ahora;
            if (!ActivacionAdminInicial.EsMarcaSinEmision(u, e!) && !EmisionCorreoCuenta.PuedeReemitir(e!.FechaCreacion, ahora))
                return ServiceResult.Error("Espere al menos 60 segundos desde la última emisión.");
            token = TokenInvitacion.Generar();
            hash = ActivacionAdminInicial.HashToken(token);
            e!.TokenHash = hash;
            e.FechaCreacion = ahora;
            e.FechaExpiracion = ahora.AddMinutes(10);
            e.FechaInvalidacion = null;
            AuditoriaService.RegistrarBootstrapAdmin(db, "ADMIN_ENLACE_EMITIDO", u.Id);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        finally { db.ChangeTracker.Clear(); }
        // SMTP siempre después del commit; el token nunca se imprime ni persiste en claro.
        if (await EnviarAsync(email, origen, token))
            return ServiceResult.Ok("SMTP aceptó el enlace. Compruebe la bandeja del destinatario; no se garantiza su entrega.");
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await BloquearAsync(ids[0]);
            var e = await db.EnlacesPasswordCliente.SingleOrDefaultAsync(e => e.UsuarioId == ids[0]);
            if (e?.TokenHash == hash && e.FechaConsumida == null)
            {
                e.FechaInvalidacion = Ahora;
                AuditoriaService.RegistrarBootstrapAdmin(db, "ADMIN_ENVIO_FALLIDO", ids[0]);
                await db.SaveChangesAsync();
            }
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning("No se confirmó la invalidación del enlace ADMIN; conserva su vencimiento. Tipo: {Tipo}.", ex.GetType().Name);
        }
        finally { db.ChangeTracker.Clear(); }
        return ServiceResult.Error("No se confirmó el envío. La cuenta sigue pendiente; reemita localmente después de 60 segundos.");
    }

    internal async Task<bool> EnviarAsync(string email, Uri origen, string token)
    {
        try
        {
            var url = new Uri(new Uri(origen.AbsoluteUri.TrimEnd('/') + "/"), "ActivacionAdmin/Activar").AbsoluteUri + "#" + token;
            await correo.EnviarCorreoAsync(email, "Activación inicial de ADMIN — MecaniCar360",
                "El enlace vence en 10 minutos y sólo puede utilizarse una vez. Ábralo en el equipo de la instalación local. " +
                "No lo comparta. <a href=\"" + System.Net.WebUtility.HtmlEncode(url) + "\">Establecer contraseña administrativa</a>");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning("No se confirmó el correo de activación ADMIN. Tipo: {Tipo}.", ex.GetType().Name);
            return false;
        }
    }
}
