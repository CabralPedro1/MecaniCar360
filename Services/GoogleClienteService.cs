using System.Data;
using System.Security.Claims;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class GoogleClienteService(MecaniCarContext db, IdentidadClienteService identidad,
    AccountService account, AuditoriaService auditoria)
{
    public const string ErrorPublico = "No se pudo completar el acceso. Si ya tiene cuenta, utilice sus credenciales o el enlace de activación y vincule Google desde el portal.";

    // Sólo el callback autenticado por el middleware oficial suministra estos claims.
    internal async Task<ServiceResult<ClaimsIdentity>> ResolverAsync(ClaimsPrincipal externa, int? vincularUsuario = null)
    {
        var verificada = IdentidadGoogleVerificada.Leer(externa);
        if (verificada == null) return ServiceResult<ClaimsIdentity>.Error(ErrorPublico);
        var sub = verificada.Subject;
        var email = verificada.Email;
        if (db.Database.CurrentTransaction != null || db.ChangeTracker.HasChanges())
            return ServiceResult<ClaimsIdentity>.Error(ErrorPublico);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await identidad.BloquearEmailAsync(email);
            var existente = await db.IdentidadesExternas.Include(i => i.Usuario).ThenInclude(u => u.Persona)
                .SingleOrDefaultAsync(i => i.Proveedor == ProveedorIdentidadExterna.Google && i.IdentificadorExterno == sub);
            Usuario? usuario;
            if (vincularUsuario.HasValue)
            {
                usuario = await db.Usuarios.Include(u => u.Persona).SingleOrDefaultAsync(u => u.Id == vincularUsuario);
                if (usuario == null || !usuario.Activo || !await identidad.ExclusivamenteClienteAsync(usuario.PersonaId) ||
                    !ClienteHabilitadoService.CredencialEstablecida(usuario.PasswordHash) ||
                    !string.Equals(usuario.EmailLogin, email, StringComparison.OrdinalIgnoreCase) ||
                    existente != null && existente.UsuarioId != usuario.Id ||
                    await db.IdentidadesExternas.AnyAsync(i => i.UsuarioId == usuario.Id && i.IdentificadorExterno != sub))
                    return ServiceResult<ClaimsIdentity>.Error(ErrorPublico);
            }
            else if (existente != null) usuario = existente.Usuario;
            else
            {
                // Nunca vincular automáticamente una Persona ni una cuenta por coincidencia de email.
                if (await db.Usuarios.AnyAsync(u => u.EmailLogin.Trim().ToUpper() == email.ToUpper()) ||
                    await db.Personas.AnyAsync(p => p.Email != null && p.Email.Trim().ToUpper() == email.ToUpper()))
                    return ServiceResult<ClaimsIdentity>.Error(ErrorPublico);
                var rol = await db.Roles.SingleOrDefaultAsync(r => r.Nombre == RolesSistema.CLIENTE && r.Activo);
                if (rol == null) return ServiceResult<ClaimsIdentity>.Error(ErrorPublico);
                static string? Nombre(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim()[..Math.Min(valor.Trim().Length, 100)];
                var persona = new Persona { Nombre = Nombre(externa.FindFirstValue(ClaimTypes.GivenName)),
                    Apellido = Nombre(externa.FindFirstValue(ClaimTypes.Surname)), Email = email };
                persona.Roles.Add(new PersonaRol { RolId = rol.Id });
                usuario = new Usuario { Persona = persona, Username = "google_" + Guid.NewGuid().ToString("N"),
                    EmailLogin = email, PasswordHash = null, PrimerLogin = false, Activo = true };
                db.Usuarios.Add(usuario);
                await db.SaveChangesAsync();
                auditoria.RegistrarOnboarding("CLIENTE_CREADO", "Persona", persona.Id);
            }
            if (!usuario.Activo || !await identidad.ExclusivamenteClienteAsync(usuario.PersonaId))
                return ServiceResult<ClaimsIdentity>.Error(ErrorPublico);
            if (existente == null)
            {
                db.IdentidadesExternas.Add(new IdentidadExterna { UsuarioId = usuario.Id,
                    Proveedor = ProveedorIdentidadExterna.Google, IdentificadorExterno = sub });
                await db.SaveChangesAsync();
                auditoria.RegistrarOnboarding("GOOGLE_VINCULADO", "Usuario", usuario.Id, vincularUsuario);
            }
            auditoria.RegistrarOnboarding("LOGIN_GOOGLE_EXITOSO", "Usuario", usuario.Id, usuario.Id);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult<ClaimsIdentity>.Ok(account.CrearIdentity(new LoginResult { Usuario = usuario,
                Roles = [RolesSistema.CLIENTE] }));
        }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.SqlClient.SqlException)
        {
            db.ChangeTracker.Clear();
            return ServiceResult<ClaimsIdentity>.Error(ErrorPublico);
        }
    }
}
