using System.Security.Cryptography;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class UsuarioService
    {
        private readonly MecaniCarContext _context;
        private readonly PermisoService _permisoService;
        private readonly EmailService _emailService;

        public UsuarioService(
            MecaniCarContext context,
            PermisoService permisoService,
            EmailService emailService)
        {
            _context = context;
            _permisoService = permisoService;
            _emailService = emailService;
        }

        public async Task<ServiceResult<List<Usuario>>> ObtenerTodosAsync(
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "USUARIO_VER"))
            {
                return ServiceResult<List<Usuario>>.Error(
                    "No posee permisos para consultar usuarios.");
            }

            var esAdmin = await _permisoService.EsAdministradorAsync(
                usuarioSolicitanteId);

            var esCaja = await EsCajaEfectivoAsync(
                usuarioSolicitanteId);

            if (!esAdmin && !esCaja)
            {
                return ServiceResult<List<Usuario>>.Error(
                    "No posee permisos para administrar cuentas.");
            }

            var query = _context.Usuarios
                .Include(u => u.Persona)
                .AsQueryable();

            if (!esAdmin)
            {
                query = query.Where(u =>
                    u.Persona.Roles.Any(pr =>
                        pr.FechaBaja == null &&
                        pr.Rol.Activo &&
                        pr.Rol.Nombre == RolesSistema.CLIENTE));
            }

            var usuarios = await query
                .OrderBy(u => u.Username)
                .ToListAsync();

            return ServiceResult<List<Usuario>>.Ok(usuarios);
        }

        public async Task<ServiceResult<Usuario>> ObtenerPorIdAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "USUARIO_VER"))
            {
                return ServiceResult<Usuario>.Error(
                    "No posee permisos para consultar usuarios.");
            }

            var usuario = await ObtenerUsuarioInternoAsync(id);

            if (usuario == null)
                return ServiceResult<Usuario>.Error(
                    "Usuario no encontrado.");

            if (!await PuedeAdministrarPersonaAsync(
                usuarioSolicitanteId,
                usuario.PersonaId,
                "USUARIO_VER"))
            {
                return ServiceResult<Usuario>.Error(
                    "No puede administrar esta cuenta.");
            }

            return ServiceResult<Usuario>.Ok(usuario);
        }

        public async Task<ServiceResult<List<Persona>>> ObtenerPersonasDisponiblesAsync(
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "USUARIO_CREAR"))
            {
                return ServiceResult<List<Persona>>.Error(
                    "No posee permisos para crear usuarios.");
            }

            var esAdmin = await _permisoService.EsAdministradorAsync(
                usuarioSolicitanteId);

            var esCaja = await EsCajaEfectivoAsync(
                usuarioSolicitanteId);

            if (!esAdmin && !esCaja)
            {
                return ServiceResult<List<Persona>>.Error(
                    "No posee permisos para administrar cuentas.");
            }

            var query = _context.Personas
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .Where(p =>
                    p.Activo &&
                    p.Usuario == null)
                .AsQueryable();

            if (!esAdmin)
            {
                query = query.Where(EsClienteActivoExpression());
            }

            var personas = await query
                .OrderBy(p => p.Apellido)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return ServiceResult<List<Persona>>.Ok(personas);
        }

        public async Task<ServiceResult> CrearAsync(
            UsuarioCreateViewModel model,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "USUARIO_CREAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para crear usuarios.");
            }

            var persona = await _context.Personas
                .FirstOrDefaultAsync(p => p.Id == model.PersonaId);

            if (persona == null || !persona.Activo)
                return ServiceResult.Error(
                    "La persona no existe o está inactiva.");

            if (!await PuedeAdministrarPersonaAsync(
                usuarioSolicitanteId,
                persona.Id,
                "USUARIO_CREAR"))
            {
                return ServiceResult.Error(
                    "No puede crear una cuenta para esta persona.");
            }

            if (await _context.Usuarios.AnyAsync(u =>
                u.PersonaId == persona.Id))
            {
                return ServiceResult.Error(
                    "La persona ya posee un usuario.");
            }

            var username = model.Username.Trim();
            var emailLogin = model.EmailLogin.Trim();

            if (await ExisteUsernameAsync(username))
                return ServiceResult.Error(
                    "Ya existe un usuario con ese nombre.");

            if (await ExisteEmailAsync(emailLogin))
                return ServiceResult.Error(
                    "Ya existe un usuario con ese email.");

            var contraseñaTemporal = GenerarContraseñaTemporal();

            var usuario = new Usuario
            {
                Username = username,
                EmailLogin = emailLogin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    contraseñaTemporal),
                Activo = true,
                PrimerLogin = true,
                PersonaId = persona.Id,
                FechaCreacion = DateTime.Now
            };

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                await _emailService.EnviarCorreoAsync(
                    emailLogin,
                    "Credenciales temporales MecaniCar360",
                    $"<p>Su usuario es <strong>{username}</strong>.</p>" +
                    $"<p>Su contraseña temporal es <strong>{contraseñaTemporal}</strong>.</p>" +
                    "<p>Deberá cambiarla en el primer ingreso.</p>");

                await transaction.CommitAsync();

                return ServiceResult.Ok(
                    "Usuario creado y credenciales enviadas correctamente.");
            }
            catch
            {
                await transaction.RollbackAsync();

                return ServiceResult.Error(
                    "No se pudo crear la cuenta porque no fue posible enviar las credenciales por email.");
            }
        }

        public async Task<ServiceResult> EditarAsync(
            UsuarioEditViewModel model,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "USUARIO_MODIFICAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar usuarios.");
            }

            var usuario = await ObtenerUsuarioInternoAsync(model.Id);

            if (usuario == null)
                return ServiceResult.Error("Usuario no encontrado.");

            if (!await PuedeAdministrarPersonaAsync(
                usuarioSolicitanteId,
                usuario.PersonaId,
                "USUARIO_MODIFICAR"))
            {
                return ServiceResult.Error(
                    "No puede modificar esta cuenta.");
            }

            var username = model.Username.Trim();
            var emailLogin = model.EmailLogin.Trim();

            if (await ExisteUsernameAsync(username, usuario.Id))
                return ServiceResult.Error(
                    "Ya existe otro usuario con ese nombre.");

            if (await ExisteEmailAsync(emailLogin, usuario.Id))
                return ServiceResult.Error(
                    "Ya existe otro usuario con ese email.");

            usuario.Username = username;
            usuario.EmailLogin = emailLogin;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Usuario actualizado correctamente.");
        }

        public async Task<ServiceResult> CambiarEstadoAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "USUARIO_DESACTIVAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar el estado de usuarios.");
            }

            var usuario = await ObtenerUsuarioInternoAsync(id);

            if (usuario == null)
                return ServiceResult.Error("Usuario no encontrado.");

            if (!await PuedeAdministrarPersonaAsync(
                usuarioSolicitanteId,
                usuario.PersonaId,
                "USUARIO_DESACTIVAR"))
            {
                return ServiceResult.Error(
                    "No puede modificar esta cuenta.");
            }

            if (usuario.Activo &&
                await _permisoService.EsAdministradorEfectivoPersonaAsync(
                    usuario.PersonaId))
            {
                if (usuario.Id == usuarioSolicitanteId)
                {
                    return ServiceResult.Error(
                        "No puede desactivar su propia cuenta ADMIN.");
                }

                if (await _permisoService
                    .ContarAdministradoresEfectivosAsync() <= 1)
                {
                    return ServiceResult.Error(
                        "No se puede desactivar el último ADMIN efectivo.");
                }
            }

            usuario.Activo = !usuario.Activo;
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                usuario.Activo
                    ? "Usuario activado correctamente."
                    : "Usuario desactivado correctamente.");
        }

        private async Task<Usuario?> ObtenerUsuarioInternoAsync(int id)
        {
            return await _context.Usuarios
                .Include(u => u.Persona)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        private async Task<bool> PuedeAdministrarPersonaAsync(
            int usuarioSolicitanteId,
            int personaId,
            string patente)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                patente))
            {
                return false;
            }

            if (await _permisoService.EsAdministradorAsync(
                usuarioSolicitanteId))
            {
                return true;
            }

            return await EsCajaEfectivoAsync(usuarioSolicitanteId) &&
                await EsClienteActivoAsync(personaId);
        }

        private async Task<bool> EsCajaEfectivoAsync(int usuarioId)
        {
            return await _context.Usuarios
                .Where(u =>
                    u.Id == usuarioId &&
                    u.Activo &&
                    u.Persona.Activo)
                .AnyAsync(u => u.Persona.Roles.Any(pr =>
                    pr.FechaBaja == null &&
                    pr.Rol.Activo &&
                    pr.Rol.Nombre == RolesSistema.CAJA));
        }

        private async Task<bool> EsClienteActivoAsync(int personaId)
        {
            return await _context.PersonaRoles.AnyAsync(pr =>
                pr.PersonaId == personaId &&
                pr.FechaBaja == null &&
                pr.Rol.Activo &&
                pr.Rol.Nombre == RolesSistema.CLIENTE &&
                pr.Persona.Activo);
        }

        private static System.Linq.Expressions.Expression<Func<Persona, bool>>
            EsClienteActivoExpression()
        {
            return p => p.Roles.Any(pr =>
                pr.FechaBaja == null &&
                pr.Rol.Activo &&
                pr.Rol.Nombre == RolesSistema.CLIENTE);
        }

        private async Task<bool> ExisteUsernameAsync(
            string username,
            int? excluirId = null)
        {
            var normalized = username.ToUpper();

            return await _context.Usuarios.AnyAsync(u =>
                u.Username.ToUpper() == normalized &&
                (!excluirId.HasValue || u.Id != excluirId.Value));
        }

        private async Task<bool> ExisteEmailAsync(
            string email,
            int? excluirId = null)
        {
            var normalized = email.ToUpper();

            return await _context.Usuarios.AnyAsync(u =>
                u.EmailLogin.ToUpper() == normalized &&
                (!excluirId.HasValue || u.Id != excluirId.Value));
        }

        private static string GenerarContraseñaTemporal()
        {
            const string minusculas = "abcdefghijkmnopqrstuvwxyz";
            const string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string numeros = "23456789";
            const string simbolos = "!@#$%";

            var caracteres = new[]
            {
                mayusculas[RandomNumberGenerator.GetInt32(mayusculas.Length)],
                minusculas[RandomNumberGenerator.GetInt32(minusculas.Length)],
                numeros[RandomNumberGenerator.GetInt32(numeros.Length)],
                simbolos[RandomNumberGenerator.GetInt32(simbolos.Length)]
            };

            var todos = mayusculas + minusculas + numeros + simbolos;
            var password = caracteres.ToList();

            while (password.Count < 12)
            {
                password.Add(
                    todos[RandomNumberGenerator.GetInt32(todos.Length)]);
            }

            return new string(password
                .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
                .ToArray());
        }
    }
}
