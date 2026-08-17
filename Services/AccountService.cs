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

            if (roles.Contains("EXEMPLEADO"))
            {
                resultado.Mensaje = "Usuario sin acceso al sistema.";
                return resultado;
            }

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

            if (usuario == null)
                return ServiceResult<Usuario>.Error("Usuario no encontrado.");

            return ServiceResult<Usuario>.Ok(usuario);
        }

        // =====================================
        // COMPLETAR DATOS
        // =====================================

        public async Task<ServiceResult> CompletarDatosAsync(
            CompletarDatosViewModel model,
            int usuarioId)
        {
            var usuario = await ObtenerUsuarioCompletoAsync(usuarioId);

            if (usuario == null)
                return ServiceResult.Error("Usuario no encontrado.");

            var validacion = ValidarCompletarDatos(model);

            if (!validacion.Exitoso)
                return validacion;

            usuario.Persona.Nombre = model.Nombre;
            usuario.Persona.Apellido = model.Apellido;
            usuario.Persona.Dni = model.Dni;
            usuario.Persona.Telefono = model.Telefono;

            usuario.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(model.Password);

            usuario.PrimerLogin = false;

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Datos actualizados correctamente.");
        }

        // =====================================
        // HELPERS
        // =====================================

        public ClaimsIdentity CrearIdentity(LoginResult login)
        {
            var usuario = login.Usuario!;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Username),
                new Claim("PersonaId", usuario.PersonaId.ToString())
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