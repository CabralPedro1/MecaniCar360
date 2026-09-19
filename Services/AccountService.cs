using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MecaniCar360.Services
{
    public class AccountService
    {
        // Hash ficticio precomputado de un valor aleatorio descartado, coste 11.
        // Nunca autentica: su verificación sólo aproxima el coste del caso inexistente/inactivo.
        private const string DummyPasswordHash = "$2a$11$/hMq7CVS2c4GIr4L3Un9KOk82Q5S7XYwehDng396O9eKwb0BeHHDC";

        private readonly MecaniCarContext _context;

        public AccountService(MecaniCarContext context)
        {
            _context = context;
        }

        // =====================================
        // LOGIN
        // =====================================

        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            var resultado = new LoginResult();

            var usuario = await ObtenerUsuarioPorUsernameAsync(username);

            if (usuario == null)
            {
                _ = BCrypt.Net.BCrypt.Verify(password, DummyPasswordHash);
                resultado.Mensaje = "Usuario o contraseña incorrectos.";
                return resultado;
            }

            if (!BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
            {
                resultado.Mensaje = "Usuario o contraseña incorrectos.";
                return resultado;
            }

            var roles = usuario.Persona.Roles
                .Where(pr =>
                    pr.FechaBaja == null &&
                    pr.Rol.Activo)
                .Select(pr => pr.Rol.Nombre)
                .ToList();

            resultado.Exitoso = true;
            resultado.Usuario = usuario;
            resultado.Roles = roles;

            return resultado;
        }

        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<Usuario>> ObtenerUsuarioAsync(int id)
        {
            var usuario = await ObtenerUsuarioCompletoAsync(id);

            if (usuario == null || !usuario.Activo || usuario.Persona == null || !usuario.Persona.Activo)
                return ServiceResult<Usuario>.Error("Usuario no encontrado.");

            return ServiceResult<Usuario>.Ok(usuario);
        }

        // =====================================
        // COMPLETAR DATOS
        // =====================================

        public async Task<ServiceResult<ClaimsIdentity>> CompletarDatosAsync(
            CompletarDatosViewModel model,
            int usuarioId)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var usuario = await BloquearUsuarioAsync(usuarioId);

            if (usuario == null || !usuario.Activo || usuario.Persona == null || !usuario.Persona.Activo)
                return ServiceResult<ClaimsIdentity>.Error("Usuario no encontrado.");

            if (!usuario.PrimerLogin)
                return ServiceResult<ClaimsIdentity>.Error(
                    "El primer ingreso ya fue completado.");

            var validacion = ValidarCompletarDatos(model);

            if (!validacion.Exitoso)
                return ServiceResult<ClaimsIdentity>.Error(validacion.Mensaje);

            usuario.Persona.Telefono = model.Telefono;

            usuario.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(model.Password);

            usuario.PrimerLogin = false;

            usuario.SecurityStamp = Guid.NewGuid().ToString("N");
            var identity = await CrearIdentityActualAsync(usuario);
            await GuardarCambiosAsync();
            await transaction.CommitAsync();

            return ServiceResult<ClaimsIdentity>.Ok(identity, "Datos actualizados correctamente.");
        }

        public async Task<ServiceResult<ClaimsIdentity>> CambiarContraseñaAsync(
            CambiarContraseñaViewModel model,
            int usuarioId)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var usuario = await BloquearUsuarioAsync(usuarioId);

            if (usuario == null || !usuario.Activo || usuario.Persona == null || !usuario.Persona.Activo)
                return ServiceResult<ClaimsIdentity>.Error("Usuario no encontrado.");

            if (!BCrypt.Net.BCrypt.Verify(
                model.ContraseñaActual,
                usuario.PasswordHash))
            {
                return ServiceResult<ClaimsIdentity>.Error(
                    "La contraseña actual no es válida.");
            }

            var validacion = PasswordValidator.EsValida(
                model.NuevaContraseña,
                out string error);

            if (!validacion)
                return ServiceResult<ClaimsIdentity>.Error(error);

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                model.NuevaContraseña);

            usuario.SecurityStamp = Guid.NewGuid().ToString("N");
            var identity = await CrearIdentityActualAsync(usuario);
            await GuardarCambiosAsync();
            await transaction.CommitAsync();

            return ServiceResult<ClaimsIdentity>.Ok(identity,
                "Contraseña actualizada correctamente.");
        }

        // =====================================
        // HELPERS
        // =====================================

        internal ClaimsIdentity CrearIdentity(LoginResult login)
        {
            var usuario = login.Usuario!;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Username),
                new Claim("PersonaId", usuario.PersonaId.ToString()),
                new Claim(Usuario.SecurityStampClaim, usuario.SecurityStamp)
            };

            claims.AddRange(
                login.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

            return new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);
        }

        // =====================================
        // MÉTODOS PRIVADOS - CONSULTAS
        // =====================================

        private async Task<Usuario?> BloquearUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FromSqlInterpolated(
                $"SELECT * FROM [Usuarios] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {usuarioId}")
                .FirstOrDefaultAsync();
            if (usuario == null) return null;
            await _context.Entry(usuario).ReloadAsync();
            await _context.Entry(usuario).Reference(u => u.Persona).LoadAsync();
            if (usuario.Persona != null) await _context.Entry(usuario.Persona).ReloadAsync();
            return usuario;
        }

        private async Task<ClaimsIdentity> CrearIdentityActualAsync(Usuario usuario)
        {
            var roles = await _context.PersonaRoles.AsNoTracking()
                .Where(pr => pr.PersonaId == usuario.PersonaId && pr.FechaBaja == null && pr.Rol.Activo)
                .Select(pr => pr.Rol.Nombre).ToListAsync();
            return CrearIdentity(new LoginResult { Usuario = usuario, Roles = roles });
        }

        private async Task<Usuario?> ObtenerUsuarioPorUsernameAsync(string username)
        {
            return await _context.Usuarios
                .Include(u => u.Persona)
                    .ThenInclude(p => p.Roles)
                        .ThenInclude(pr => pr.Rol)
                .FirstOrDefaultAsync(u =>
                    u.Username == username &&
                    u.Activo &&
                    u.Persona.Activo);
        }

        private async Task<Usuario?> ObtenerUsuarioCompletoAsync(int id)
        {
            return await _context.Usuarios
                .Include(u => u.Persona)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        // =====================================
        // MÉTODOS PRIVADOS - VALIDACIONES
        // =====================================

        private ServiceResult ValidarCompletarDatos(
            CompletarDatosViewModel model)
        {
            if (!PasswordValidator.EsValida(model.Password, out string error))
                return ServiceResult.Error(error);

            return ServiceResult.Ok();
        }

        // =====================================
        // MÉTODOS PRIVADOS - OPERACIONES
        // =====================================

        private async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
