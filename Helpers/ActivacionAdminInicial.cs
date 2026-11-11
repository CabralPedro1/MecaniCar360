using MecaniCar360.Models;

namespace MecaniCar360.Helpers;

// Reutiliza el formato persistido, nunca la autoridad del flujo CLIENTE.
internal static class ActivacionAdminInicial
{
    private const string Proposito = "MecaniCar360.BootstrapAdmin.Activacion.v1:";
    internal static string HashToken(string token) => TokenInvitacion.Hash(Proposito + "token:" + token);
    private static string Stamp(Usuario u) => TokenInvitacion.Hash(Proposito + "stamp:" + u.SecurityStamp);
    private static string Email(Usuario u) => TokenInvitacion.Hash(Proposito + "email:" + IdentificadorCuenta.ClaveEmailInvariante(u.EmailLogin));
    internal static string EmailConfigurado(IConfiguration config)
    {
        var email = IdentificadorCuenta.Normalizar(config["BootstrapAdmin:Email"]);
        if (!IdentificadorCuenta.EmailValido(email))
            throw new InvalidOperationException("Configure BootstrapAdmin:Email explícitamente para la instalación nueva.");
        return email;
    }

    internal static EnlacePasswordCliente CrearMarca(Usuario u, DateTime ahora) => new()
    {
        Usuario = u, UsuarioId = u.Id, Finalidad = FinalidadPasswordCliente.Agregar,
        TeniaPassword = false, StampHash = Stamp(u), EmailHash = Email(u),
        TokenHash = TokenInvitacion.Hash(Proposito + "pendiente:" + u.SecurityStamp),
        FechaCreacion = ahora, FechaExpiracion = ahora.AddMinutes(10), FechaInvalidacion = ahora
    };

    internal static bool EsMarcaSinEmision(Usuario u, EnlacePasswordCliente e) =>
        e.TokenHash == TokenInvitacion.Hash(Proposito + "pendiente:" + u.SecurityStamp);

    internal static bool EsPendiente(Usuario u, EnlacePasswordCliente? e, string emailConfigurado) =>
        e != null && e.UsuarioId == u.Id && !u.Activo && u.Persona.Activo &&
        u.PasswordHash == null && u.PrimerLogin && e.FechaConsumida == null &&
        e.Finalidad == FinalidadPasswordCliente.Agregar && !e.TeniaPassword &&
        e.StampHash == Stamp(u) && e.EmailHash == Email(u) &&
        IdentificadorCuenta.ClaveEmailInvariante(u.EmailLogin) == IdentificadorCuenta.ClaveEmailInvariante(emailConfigurado) &&
        u.Persona.Roles.Any(r => r.FechaBaja == null && r.Rol.Activo && r.Rol.Nombre == RolesSistema.ADMIN) &&
        !u.Persona.Roles.Any(r => r.FechaBaja == null && r.Rol.Activo && r.Rol.Nombre != RolesSistema.ADMIN);

    internal static bool EsVigente(Usuario u, EnlacePasswordCliente? e, string email, DateTime ahora) =>
        EsPendiente(u, e, email) && e!.FechaInvalidacion == null && e.FechaCreacion <= ahora && e.FechaExpiracion > ahora;
}
