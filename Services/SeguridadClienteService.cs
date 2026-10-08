using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

// Prueba reciente de acceso al correo ya verificado. No es una invitación ni inicia sesión.
public sealed class SeguridadClienteService(MecaniCarContext db, IdentidadClienteService identidad,
    ClienteHabilitadoService habilitado, IDataProtectionProvider proteccion, EmailService correo,
    AuditoriaService auditoria, ILogger<SeguridadClienteService> logger, IConfiguration config, IWebHostEnvironment ambiente)
{
    private sealed record Comprobante(int UsuarioId, string Stamp, string Email);
    private ITimeLimitedDataProtector Protector(string uso) => proteccion
        .CreateProtector("MecaniCar360.Cliente.PruebaCorreo.v1", uso).ToTimeLimitedDataProtector();

    private async Task<Usuario?> ClienteAsync(int id)
    {
        var u = await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id && u.Activo && u.Persona.Activo);
        return u != null && await identidad.ExclusivamenteClienteAsync(u.PersonaId) &&
            await habilitado.CorreoVerificadoAsync(u) ? u : null;
    }

    private async Task<bool> EnviarAsync(Usuario u, string uso)
    {
        var codigo = Protector(uso).Protect(JsonSerializer.Serialize(new Comprobante(u.Id, u.SecurityStamp, u.EmailLogin)),
            TimeSpan.FromMinutes(10));
        try
        {
            await correo.EnviarCorreoAsync(u.EmailLogin, "Verificación de acceso MecaniCar",
                "Use este código en la pantalla de verificación que abrió en MecaniCar. Vence en 10 minutos. " +
                "No lo comparta. Si no solicitó esta operación, ignore este mensaje.<br><code>" +
                System.Net.WebUtility.HtmlEncode(codigo) + "</code>");
            logger.LogInformation(new EventId(4110, "CLIENTE_VERIFICACION_ENVIADA"),
                "SMTP aceptó un mensaje de verificación de cliente. Propósito: {Uso}.", uso);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(new EventId(4111, "CLIENTE_VERIFICACION_ENVIO_FALLIDO"),
                "No se confirmó el envío de verificación. Tipo: {Tipo}.", ex.GetType().Name);
            return false;
        }
    }

    internal async Task EnviarOrientacionAsync(int id)
    {
        var u = await ClienteAsync(id);
        if (u != null) await EnviarAsync(u, "orientacion");
    }

    private async Task<Usuario?> VerificarAsync(string? codigo, string uso)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length > 4096) return null;
        try
        {
            var prueba = JsonSerializer.Deserialize<Comprobante>(Protector(uso).Unprotect(codigo.Trim()));
            if (prueba == null) return null;
            var u = await ClienteAsync(prueba.UsuarioId);
            return u != null && u.SecurityStamp == prueba.Stamp && u.EmailLogin == prueba.Email ? u : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException) { return null; }
    }

    public async Task<string?> OrientarAsync(string codigo)
    {
        var u = await VerificarAsync(codigo, "orientacion");
        if (u == null) return null;
        var google = await db.IdentidadesExternas.AnyAsync(i => i.UsuarioId == u.Id &&
            i.Proveedor == Models.Enums.ProveedorIdentidadExterna.Google);
        return google
            ? "Su cuenta ya existe. Inicie sesión mediante Google. Desde el portal puede administrar su contraseña local."
            : "Su cuenta ya existe. Inicie sesión con su correo y contraseña. Esta solicitud no cambia sus credenciales.";
    }

    public const string RespuestaNeutra = "Si la cuenta corresponde y el envío puede completarse, recibirá un enlace en su correo. Revise su bandeja y correo no deseado.";
    public const string EnlaceNoValido = "El enlace no está disponible. Solicite uno nuevo.";

    public async Task<bool> PuedeGestionarAsync(int actor)
    {
        var u = await ClienteAsync(actor);
        return u != null && await habilitado.EstaHabilitadoAsync(u.PersonaId);
    }

    public async Task SolicitarRecuperacionAsync(string email)
    {
        var normalizado = IdentificadorCuenta.Normalizar(email).ToUpperInvariant();
        if (!IdentificadorCuenta.EmailValido(normalizado)) return;
        try
        {
            var id = await db.Usuarios.Where(u => u.EmailLogin.Trim().ToUpper() == normalizado)
                .Select(u => (int?)u.Id).SingleOrDefaultAsync();
            if (id.HasValue) await EmitirEnlaceAsync(id.Value, true);
        }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.SqlClient.SqlException)
        {
            logger.LogWarning("No se pudo procesar la recuperación. Tipo: {Tipo}.", ex.GetType().Name);
        }
    }

    public Task<bool> SolicitarPasswordAsync(int actor) => EmitirEnlaceAsync(actor, false);

    private static string HashEmail(string email) => TokenInvitacion.Hash(email.Trim().ToUpperInvariant());
    private bool Disponible() => db.Database.CurrentTransaction == null && !db.ChangeTracker.HasChanges();
    private async Task BloquearPersonaAsync(int personaId) => await db.Personas.FromSqlInterpolated(
        $"SELECT * FROM [Personas] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={personaId}").AsNoTracking().SingleAsync();

    private async Task<bool> EmitirEnlaceAsync(int actor, bool recuperacion)
    {
        if (!Disponible()) return false;
        // No derivar el destino del Host del request: sólo configuración confiable.
        if (!Uri.TryCreate(config["Invitaciones:UrlBase"], UriKind.Absolute, out var baseUri) ||
            !string.IsNullOrEmpty(baseUri.UserInfo) || !string.IsNullOrEmpty(baseUri.Query) || !string.IsNullOrEmpty(baseUri.Fragment) ||
            !(baseUri.Scheme == "https" || (ambiente.IsDevelopment() && baseUri.Scheme == "http" && baseUri.IsLoopback)))
        {
            logger.LogWarning("Enlace de contraseña no emitido: URL base inválida.");
            return false;
        }
        var previo = await ClienteAsync(actor);
        if (previo == null) return false;
        var token = TokenInvitacion.Generar();
        var hash = TokenInvitacion.Hash(token);
        string email;
        var originales = Capturar(); var confirmado = false;
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await BloquearPersonaAsync(previo.PersonaId);
            var u = await ClienteAsync(actor);
            if (u == null || !await habilitado.EstaHabilitadoAsync(u.PersonaId)) return false;
            email = u.EmailLogin;
            var enlace = await db.EnlacesPasswordCliente.SingleOrDefaultAsync(e => e.UsuarioId == actor);
            if (enlace == null) { enlace = new() { UsuarioId = actor }; db.EnlacesPasswordCliente.Add(enlace); }
            else await db.Entry(enlace).ReloadAsync();
            enlace.TokenHash = hash; enlace.StampHash = TokenInvitacion.Hash(u.SecurityStamp);
            enlace.EmailHash = HashEmail(u.EmailLogin); enlace.TeniaPassword = u.PasswordHash != null;
            enlace.Finalidad = recuperacion ? FinalidadPasswordCliente.Recuperar : enlace.TeniaPassword
                ? FinalidadPasswordCliente.Cambiar : FinalidadPasswordCliente.Agregar;
            enlace.FechaCreacion = DateTime.UtcNow; enlace.FechaExpiracion = enlace.FechaCreacion.AddMinutes(10);
            enlace.FechaConsumida = null; enlace.FechaInvalidacion = null;
            // Reemplazar el hash invalida cualquier enlace anterior, incluso de otra finalidad.
            await db.SaveChangesAsync(); await tx.CommitAsync(); confirmado = true;
        }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.SqlClient.SqlException)
        {
            logger.LogWarning("No se pudo emitir el enlace. Tipo: {Tipo}.", ex.GetType().Name);
            return false;
        }
        finally { Restaurar(originales, confirmado); }
        try
        {
            var url = new Uri(new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/"), "SeguridadCliente/Restablecer").AbsoluteUri + "#" + token;
            await correo.EnviarCorreoAsync(email, "Gestionar contraseña MecaniCar",
                "Para establecer su contraseña abra este enlace. Vence en 10 minutos y sólo puede usarse una vez. " +
                "Si no solicitó la operación, ignore el correo. <a href=\"" + System.Net.WebUtility.HtmlEncode(url) + "\">Establecer contraseña</a>");
            logger.LogInformation(new EventId(4112, "CLIENTE_ENLACE_PASSWORD_ENVIADO"), "SMTP aceptó un enlace de contraseña.");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning("No se confirmó el envío del enlace. Tipo: {Tipo}.", ex.GetType().Name);
            // No invalidar una reemisión posterior. La tabla no contiene credenciales ni entidades DVH.
            try
            {
                await db.EnlacesPasswordCliente.Where(e => e.UsuarioId == actor && e.TokenHash == hash && e.FechaConsumida == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.FechaInvalidacion, DateTime.UtcNow));
            }
            catch (Microsoft.Data.SqlClient.SqlException)
            {
                logger.LogWarning("No se pudo invalidar el enlace tras el fallo de envío; conserva su vencimiento original.");
            }
            return false;
        }
    }

    private async Task<Usuario?> ValidarCoreAsync(EnlacePasswordCliente? enlace)
    {
        if (enlace == null || enlace.FechaConsumida != null || enlace.FechaInvalidacion != null ||
            enlace.FechaExpiracion <= DateTime.UtcNow || !Enum.IsDefined(enlace.Finalidad)) return null;
        var u = await ClienteAsync(enlace.UsuarioId);
        if (u == null || !await habilitado.EstaHabilitadoAsync(u.PersonaId) ||
            enlace.StampHash != TokenInvitacion.Hash(u.SecurityStamp) || enlace.EmailHash != HashEmail(u.EmailLogin) ||
            enlace.TeniaPassword != (u.PasswordHash != null) ||
            (enlace.Finalidad == FinalidadPasswordCliente.Agregar && enlace.TeniaPassword) ||
            (enlace.Finalidad == FinalidadPasswordCliente.Cambiar && !enlace.TeniaPassword)) return null;
        return u;
    }

    public async Task<bool> ValidarEnlaceAsync(string? token)
    {
        if (!TokenInvitacion.FormatoValido(token)) return false;
        var hash = TokenInvitacion.Hash(token!);
        return await ValidarCoreAsync(await db.EnlacesPasswordCliente.AsNoTracking().SingleOrDefaultAsync(e => e.TokenHash == hash)) != null;
    }

    public async Task<ServiceResult> RestablecerAsync(RestablecerPasswordClienteViewModel vm)
    {
        if (!TokenInvitacion.FormatoValido(vm.Token) || !Disponible()) return ServiceResult.Error(EnlaceNoValido);
        if (vm.Password != vm.ConfirmarPassword || !PasswordValidator.EsValida(vm.Password, out _))
            return ServiceResult.Error("Revise la contraseña y su confirmación.");
        var hash = TokenInvitacion.Hash(vm.Token);
        var localizador = await db.EnlacesPasswordCliente.AsNoTracking().Where(e => e.TokenHash == hash)
            .Select(e => new { e.UsuarioId, e.Usuario.PersonaId }).SingleOrDefaultAsync();
        if (localizador == null) return ServiceResult.Error(EnlaceNoValido);
        var originales = Capturar(); var confirmado = false;
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await BloquearPersonaAsync(localizador.PersonaId);
            var u = await db.Usuarios.SingleAsync(u => u.Id == localizador.UsuarioId);
            await db.Entry(u).ReloadAsync();
            var enlace = await db.EnlacesPasswordCliente.SingleOrDefaultAsync(e => e.UsuarioId == u.Id);
            if (enlace != null) await db.Entry(enlace).ReloadAsync();
            if (enlace?.TokenHash != hash || await ValidarCoreAsync(enlace) == null) return ServiceResult.Error(EnlaceNoValido);
            var agregada = u.PasswordHash == null;
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password);
            u.SecurityStamp = Guid.NewGuid().ToString("N");
            enlace.FechaConsumida = DateTime.UtcNow;
            auditoria.RegistrarPasswordCliente(u.Id, agregada);
            await db.SaveChangesAsync(); await tx.CommitAsync(); confirmado = true;
            return ServiceResult.Ok("Contraseña establecida. Inicie sesión nuevamente.");
        }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.SqlClient.SqlException)
        {
            return ServiceResult.Error("No se pudo completar la operación. Solicite otro enlace.");
        }
        finally { Restaurar(originales, confirmado); }
    }

    private Dictionary<object, Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues> Capturar() =>
        db.ChangeTracker.Entries().ToDictionary(e => e.Entity, e => e.CurrentValues.Clone(), ReferenceEqualityComparer.Instance);
    private void Restaurar(Dictionary<object, Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues> originales, bool confirmado)
    {
        foreach (var e in db.ChangeTracker.Entries().ToList())
            if (!originales.TryGetValue(e.Entity, out var valores)) e.State = EntityState.Detached;
            else if (!confirmado) { e.CurrentValues.SetValues(valores); e.OriginalValues.SetValues(valores); e.State = EntityState.Unchanged; }
    }
}
