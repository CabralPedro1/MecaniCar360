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
        private readonly AuditoriaService _auditoria;
        private readonly PermisoService _permisoService;
        private readonly EmailService _emailService;

        public UsuarioService(
            MecaniCarContext context,
            PermisoService permisoService,
            EmailService emailService, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
            _permisoService = permisoService;
            _emailService = emailService;
        }

        public async Task<ServiceResult<List<PersonaCuentaViewModel>>> ObtenerAdministracionAsync(int actorId, int? personaId = null)
        {
            if (!await _permisoService.TienePermisoAsync(actorId, "PERSONA_VER") ||
                !await _permisoService.TienePermisoAsync(actorId, "USUARIO_VER"))
                return ServiceResult<List<PersonaCuentaViewModel>>.Error("Acceso denegado.");

            var esAdmin = await _permisoService.EsAdministradorAsync(actorId);
            if (!esAdmin && !await EsCajaEfectivoAsync(actorId))
                return ServiceResult<List<PersonaCuentaViewModel>>.Error("Acceso denegado.");

            var query = _context.Personas.AsNoTracking().AsQueryable();
            if (!esAdmin)
                query = query.Where(p => p.Roles.Any(pr => pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.CLIENTE) &&
                    !p.Roles.Any(pr => pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre != RolesSistema.CLIENTE));

            // Keep internal personnel with historical assignments reachable for reassignment.
            query = query.Where(p => p.Roles.Any(pr => RolesSistema.Internos.Contains(pr.Rol.Nombre)) ||
                p.Roles.Any(pr => pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.CLIENTE));
            if (personaId.HasValue) query = query.Where(p => p.Id == personaId.Value);

            var personas = await query.OrderBy(p => p.Apellido).ThenBy(p => p.Nombre).ThenBy(p => p.Id)
                .Select(p => new PersonaCuentaViewModel
                {
                    PersonaId = p.Id, Nombre = p.Nombre, Apellido = p.Apellido, Dni = p.Dni,
                    Telefono = p.Telefono, Email = p.Email, PersonaActiva = p.Activo,
                    UsuarioId = p.Usuario == null ? null : (int?)p.Usuario.Id,
                    Username = p.Usuario == null ? null : p.Usuario.Username,
                    EmailLogin = p.Usuario == null ? null : p.Usuario.EmailLogin,
                    UsuarioActivo = p.Usuario == null ? null : (bool?)p.Usuario.Activo,
                    CredencialLocalDisponible = p.Usuario != null && p.Usuario.PasswordHash != null && p.Usuario.PasswordHash.Trim() != "",
                    GoogleVinculado = p.Usuario != null && p.Usuario.IdentidadesExternas.Any(i => i.Proveedor == MecaniCar360.Models.Enums.ProveedorIdentidadExterna.Google),
                    EsCliente = p.Roles.Any(pr => pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.CLIENTE),
                    EsPersonal = p.Roles.Any(pr => RolesSistema.Internos.Contains(pr.Rol.Nombre)),
                    Roles = p.Roles.OrderBy(pr => pr.Rol.Nombre).Select(pr => new RolCuentaViewModel
                    { Nombre = pr.Rol.Nombre, Activo = pr.Rol.Activo, FechaBaja = pr.FechaBaja }).ToList()
                }).ToListAsync();
            return ServiceResult<List<PersonaCuentaViewModel>>.Ok(personas);
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
                        pr.Rol.Nombre == RolesSistema.CLIENTE) &&
                    !u.Persona.Roles.Any(pr => pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre != RolesSistema.CLIENTE));
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
                .Where(EsElegibleCuentaInterna())
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

            if (!await _context.Personas.Where(EsElegibleCuentaInterna()).AnyAsync(p => p.Id == persona.Id))
                return ServiceResult.Error("Solo se pueden crear credenciales para personas con rol interno vigente y sin rol CLIENTE.");

            var username = IdentificadorCuenta.Normalizar(model.Username);
            var emailLogin = IdentificadorCuenta.Normalizar(model.EmailLogin);
            if (!IdentificadorCuenta.UsernameValido(username) || !IdentificadorCuenta.EmailValido(emailLogin))
                return ServiceResult.Error("Usuario o email no validos; el usuario no admite @.");

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
                _auditoria.RegistrarOperacion("USUARIO_CREADO", "Usuario", usuario.Id, usuarioSolicitanteId);
                await _context.SaveChangesAsync();

                await EnviarCredencialesAsync(emailLogin, username, contraseñaTemporal);

                await transaction.CommitAsync();

                return ServiceResult.Ok(
                    "Usuario creado y credenciales enviadas correctamente.");
            }
            catch
            {
                await transaction.RollbackAsync();

                return ServiceResult.Error(
                    "No se pudo completar el alta de la cuenta.");
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

            var username = IdentificadorCuenta.Normalizar(model.Username);
            var emailLogin = IdentificadorCuenta.Normalizar(model.EmailLogin);
            if (!IdentificadorCuenta.UsernameValido(username) || !IdentificadorCuenta.EmailValido(emailLogin))
                return ServiceResult.Error("Usuario o email no validos; el usuario no admite @.");

            if (await ExisteUsernameAsync(username, usuario.Id))
                return ServiceResult.Error(
                    "Ya existe otro usuario con ese nombre.");

            if (await ExisteEmailAsync(emailLogin, usuario.Id))
                return ServiceResult.Error(
                    "Ya existe otro usuario con ese email.");

            usuario.Username = username;
            usuario.EmailLogin = emailLogin;

            _auditoria.RegistrarOperacion("USUARIO_MODIFICADO", "Usuario", usuario.Id, usuarioSolicitanteId);
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

            await using var transaction = await _context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await _permisoService.BloquearAdministradoresAsync();

            // Revalidar la autoridad con el estado actualizado bajo el mutex.
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
            _auditoria.RegistrarOperacion(usuario.Activo ? "USUARIO_ACTIVADO" : "USUARIO_DESACTIVADO", "Usuario", usuario.Id, usuarioSolicitanteId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

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

        private static System.Linq.Expressions.Expression<Func<Persona, bool>> EsElegibleCuentaInterna()
            => p => p.Activo &&
                !p.Roles.Any(pr => pr.FechaBaja == null && pr.Rol.Nombre == RolesSistema.CLIENTE) &&
                p.Roles.Any(pr => pr.FechaBaja == null && pr.Rol.Activo &&
                    (pr.Rol.Nombre == RolesSistema.ADMIN || pr.Rol.Nombre == RolesSistema.MECANICO ||
                     pr.Rol.Nombre == RolesSistema.CAJA || pr.Rol.Nombre == RolesSistema.STOCK));

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
                await EsClienteActivoAsync(personaId) &&
                !await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == personaId && pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre != RolesSistema.CLIENTE);
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
                pr.Rol.Nombre == RolesSistema.CLIENTE) && !p.Roles.Any(pr => pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre != RolesSistema.CLIENTE);
        }

        internal async Task<bool> ExisteUsernameAsync(
            string username,
            int? excluirId = null)
        {
            var normalized = IdentificadorCuenta.Normalizar(username);

            return await _context.Usuarios.AnyAsync(u =>
                u.Username == normalized &&
                (!excluirId.HasValue || u.Id != excluirId.Value));
        }

        internal async Task<bool> ExisteEmailAsync(
            string email,
            int? excluirId = null)
        {
            var normalized = IdentificadorCuenta.Normalizar(email);

            return await _context.Usuarios.AnyAsync(u =>
                u.EmailLogin == normalized &&
                (!excluirId.HasValue || u.Id != excluirId.Value));
        }

        internal Task EnviarCredencialesAsync(string email, string username, string password)
            => _emailService.EnviarCorreoAsync(email, "Credenciales temporales MecaniCar360",
                $"<p>Su usuario es <strong>{System.Net.WebUtility.HtmlEncode(username)}</strong>.</p>" +
                $"<p>Su contraseña temporal es <strong>{System.Net.WebUtility.HtmlEncode(password)}</strong>.</p>" +
                "<p>Deberá cambiarla en el primer ingreso.</p>");

        internal static string GenerarContraseñaTemporal()
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
