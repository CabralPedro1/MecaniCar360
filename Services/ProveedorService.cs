using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class ProveedorService
    {
        private readonly MecaniCarContext _context;

        public ProveedorService(MecaniCarContext context)
        {
            _context = context;
        }

        //====================================
        // CONSULTAS
        //====================================

        public async Task<ServiceResult<List<Proveedor>>> ObtenerTodosAsync()
        {
            var proveedores = await _context.Proveedores
                .Include(p => p.Repuestos)
                    .ThenInclude(pr => pr.Repuesto)
                .OrderBy(p => p.Nombre)
                .ThenBy(p => p.Apellido)
                .ToListAsync();

            return ServiceResult<List<Proveedor>>.Ok(proveedores);
        }

        public async Task<ServiceResult<Proveedor>> ObtenerPorIdAsync(int id)
        {
            var proveedor = await ObtenerProveedorCompletoAsync(id);

            if (proveedor == null)
                return ServiceResult<Proveedor>.Error("Proveedor no encontrado.");

            return ServiceResult<Proveedor>.Ok(proveedor);
        }

        //====================================
        // MÉTODOS PRIVADOS
        //====================================

        private async Task<bool> ExisteProveedorAsync(string nombre, int? excluirId = null)
        {
            nombre = nombre.Trim().ToUpper();

            return await _context.Proveedores.AnyAsync(p =>
                p.Nombre.ToUpper() == nombre &&
                (!excluirId.HasValue || p.Id != excluirId.Value));
        }

        private async Task<Proveedor?> ObtenerProveedorCompletoAsync(int id)
        {
            return await _context.Proveedores
                .Include(p => p.Repuestos)
                    .ThenInclude(pr => pr.Repuesto)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        //====================================
        // ABM
        //====================================

        public async Task<ServiceResult> CrearAsync(Proveedor proveedor)
        {
            if (string.IsNullOrWhiteSpace(proveedor.Nombre))
                return ServiceResult.Error("Debe ingresar el nombre.");

            if (await ExisteProveedorAsync(proveedor.Nombre))
                return ServiceResult.Error("Ya existe un proveedor con ese nombre.");

            proveedor.Nombre = proveedor.Nombre.Trim();
            proveedor.Apellido = proveedor.Apellido?.Trim();
            proveedor.FechaCreacion = DateTime.UtcNow;
            proveedor.Activo = true;

            _context.Proveedores.Add(proveedor);

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Proveedor creado correctamente.");
        }

        public async Task<ServiceResult> EditarAsync(Proveedor proveedor)
        {
            var existente = await _context.Proveedores.FindAsync(proveedor.Id);

            if (existente == null)
                return ServiceResult.Error("Proveedor no encontrado.");

            if (await ExisteProveedorAsync(proveedor.Nombre, proveedor.Id))
                return ServiceResult.Error("Ya existe un proveedor con ese nombre.");

            existente.Nombre = proveedor.Nombre.Trim();
            existente.Apellido = proveedor.Apellido?.Trim();
            existente.Telefono = proveedor.Telefono;
            existente.Email = proveedor.Email;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Proveedor actualizado correctamente.");
        }

        public async Task<ServiceResult> CambiarEstadoAsync(int id)
        {
            var proveedor = await ObtenerProveedorCompletoAsync(id);

            if (proveedor == null)
                return ServiceResult.Error("Proveedor no encontrado.");

            if (proveedor.Activo && proveedor.Repuestos.Any())
                return ServiceResult.Error("No puede desactivarse porque tiene repuestos asociados.");

            proveedor.Activo = !proveedor.Activo;

            await GuardarCambiosAsync();

            return ServiceResult.Ok(
                proveedor.Activo
                    ? "Proveedor activado correctamente."
                    : "Proveedor desactivado correctamente.");
        }

        private async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}