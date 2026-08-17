using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class RolService
    {
        private readonly MecaniCarContext _context;

        public RolService(MecaniCarContext context)
        {
            _context = context;
        }

        // =============================
        // CONSULTAS
        // =============================

        public async Task<ServiceResult<List<Rol>>> ObtenerTodosAsync()
        {
            var roles = await _context.Roles
                .OrderBy(r => r.Nombre)
                .ToListAsync();

            return ServiceResult<List<Rol>>.Ok(roles);
        }

        public async Task<ServiceResult<List<Rol>>> ObtenerActivosAsync()
        {
            var roles = await _context.Roles
                .Where(r => r.Activo)
                .OrderBy(r => r.Nombre)
                .ToListAsync();

            return ServiceResult<List<Rol>>.Ok(roles);
        }

        public async Task<ServiceResult<Rol>> ObtenerPorIdAsync(int id)
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

        public async Task<ServiceResult> CrearAsync(Rol rol)
        {
            if (await ExisteNombreAsync(rol.Nombre))
                return ServiceResult.Error("Ya existe un rol con ese nombre.");

            rol.Nombre = rol.Nombre.Trim().ToUpper();
            rol.FechaCreacion = DateTime.Now;

            _context.Roles.Add(rol);

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Rol creado correctamente.");
        }

        public async Task<ServiceResult> EditarAsync(Rol rol)
        {
            var existente = await _context.Roles.FindAsync(rol.Id);

            if (existente == null)
                return ServiceResult.Error("Rol no encontrado.");

            if (!existente.Activo)
                return ServiceResult.Error("No se puede editar un rol desactivado.");

            if (await ExisteNombreAsync(rol.Nombre, rol.Id))
                return ServiceResult.Error("Ya existe otro rol con ese nombre.");

            existente.Nombre = rol.Nombre.Trim().ToUpper();
            existente.EsRolCliente = rol.EsRolCliente;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Rol actualizado correctamente.");
        }

        public async Task<ServiceResult> CambiarEstadoAsync(int id)
        {
            var rol = await _context.Roles.FindAsync(id);

            if (rol == null)
                return ServiceResult.Error("Rol no encontrado.");

            rol.Activo = !rol.Activo;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                rol.Activo
                    ? "Rol activado correctamente."
                    : "Rol desactivado correctamente.");
        }
    }
}