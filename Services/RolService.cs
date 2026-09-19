using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class RolService
    {
        private readonly MecaniCarContext _context;
        private readonly PermisoService _permisoService;
        private readonly AuditoriaService _auditoria;

        public RolService(
            MecaniCarContext context,
            PermisoService permisoService, AuditoriaService auditoria)
        {
            _context = context;
            _permisoService = permisoService;
            _auditoria = auditoria;
        }

        // =============================
        // CONSULTAS
        // =============================

        public async Task<ServiceResult<List<Rol>>> ObtenerTodosAsync(
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "ROL_VER"))
            {
                return ServiceResult<List<Rol>>.Error(
                    "No posee permisos para consultar roles.");
            }

            var roles = await _context.Roles
                .OrderBy(r => r.Nombre)
                .ToListAsync();

            return ServiceResult<List<Rol>>.Ok(roles);
        }

        public async Task<ServiceResult<List<Rol>>> ObtenerActivosAsync(int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "ROL_VER")) return ServiceResult<List<Rol>>.Error("Acceso denegado.");

            var roles = await _context.Roles
                .Where(r => r.Activo)
                .OrderBy(r => r.Nombre)
                .ToListAsync();

            return ServiceResult<List<Rol>>.Ok(roles);
        }

        public async Task<ServiceResult<Rol>> ObtenerPorIdAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "ROL_VER"))
            {
                return ServiceResult<Rol>.Error(
                    "No posee permisos para consultar roles.");
            }

            return await ObtenerPorIdInternoAsync(id);
        }

        public async Task<ServiceResult<Rol>> ObtenerPorIdParaEditarAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "ROL_MODIFICAR"))
            {
                return ServiceResult<Rol>.Error(
                    "No posee permisos para modificar roles.");
            }

            return await ObtenerPorIdInternoAsync(id);
        }

        private async Task<ServiceResult<Rol>> ObtenerPorIdInternoAsync(
            int id)
        {
            var rol = await _context.Roles
                .Include(r => r.Personas)
                    .ThenInclude(pr => pr.Persona)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rol == null)
                return ServiceResult<Rol>.Error("Rol no encontrado.");

            return ServiceResult<Rol>.Ok(rol);
        }

        private async Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null)
        {
            nombre = nombre.Trim().ToUpper();

            return await _context.Roles.AnyAsync(r =>
                r.Nombre.ToUpper() == nombre &&
                (!excluirId.HasValue || r.Id != excluirId.Value));
        }

        // =============================
        // ABM
        // =============================

        public async Task<ServiceResult> CrearAsync(
            Rol rol,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "ROL_CREAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para crear roles.");
            }

            if (rol.Nombre.Trim().Equals(
                RolesSistema.ADMIN,
                StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Error(
                    "ADMIN es un rol reservado.");
            }

            if (await ExisteNombreAsync(rol.Nombre))
                return ServiceResult.Error("Ya existe un rol con ese nombre.");

            rol.Nombre = rol.Nombre.Trim().ToUpper();
            rol.FechaCreacion = DateTime.Now;

            rol.Id = 0; rol.Familias = new(); rol.Personas = new();
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.Roles.Add(rol);

            await _context.SaveChangesAsync();
            _auditoria.RegistrarOperacion("ROL_CREADO", "Rol", rol.Id, usuarioSolicitanteId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult.Ok("Rol creado correctamente.");
        }

        public async Task<ServiceResult> EditarAsync(
            Rol rol,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "ROL_MODIFICAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar roles.");
            }

            var existente = await _context.Roles.FindAsync(rol.Id);

            if (existente == null)
                return ServiceResult.Error("Rol no encontrado.");

            if (!existente.Activo)
                return ServiceResult.Error("No se puede editar un rol desactivado.");

            if (existente.Nombre.Equals(
                RolesSistema.ADMIN,
                StringComparison.OrdinalIgnoreCase) ||
                rol.Nombre.Trim().Equals(
                    RolesSistema.ADMIN,
                    StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Error(
                    "ADMIN es un rol reservado.");
            }

            if (await ExisteNombreAsync(rol.Nombre, rol.Id))
                return ServiceResult.Error("Ya existe otro rol con ese nombre.");

            existente.Nombre = rol.Nombre.Trim().ToUpper();
            existente.EsRolCliente = rol.EsRolCliente;

            _auditoria.RegistrarOperacion("ROL_MODIFICADO", "Rol", existente.Id, usuarioSolicitanteId,
                "Rol modificado.");
            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Rol actualizado correctamente.");
        }

        public async Task<ServiceResult> CambiarEstadoAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "ROL_DESACTIVAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar el estado de roles.");
            }

            var rol = await _context.Roles.FindAsync(id);

            if (rol == null)
                return ServiceResult.Error("Rol no encontrado.");

            if (rol.Nombre.Equals(
                RolesSistema.ADMIN,
                StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Error(
                    "El rol ADMIN no puede desactivarse.");
            }

            rol.Activo = !rol.Activo;

            _auditoria.RegistrarOperacion(rol.Activo ? "ROL_ACTIVADO" : "ROL_DESACTIVADO",
                "Rol", rol.Id, usuarioSolicitanteId);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                rol.Activo
                    ? "Rol activado correctamente."
                    : "Rol desactivado correctamente.");
        }
    }
}
