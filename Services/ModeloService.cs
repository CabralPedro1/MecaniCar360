using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class ModeloService
    {
        private readonly MecaniCarContext _context;

        public ModeloService(MecaniCarContext context)
        {
            _context = context;
        }

        //====================================
        // CONSULTAS
        //====================================

        public async Task<ServiceResult<List<Modelo>>> ObtenerTodosAsync()
        {
            var modelos = await _context.Modelos
                .Include(m => m.Marca)
                .Include(m => m.Vehiculos)
                .OrderBy(m => m.Marca.Nombre)
                .ThenBy(m => m.Nombre)
                .ToListAsync();

            return ServiceResult<List<Modelo>>.Ok(modelos);
        }

        public async Task<ServiceResult<Modelo>> ObtenerPorIdAsync(int id)
        {
            var modelo = await ObtenerModeloAsync(id);

            if (modelo == null)
                return ServiceResult<Modelo>.Error("Modelo no encontrado.");

            return ServiceResult<Modelo>.Ok(modelo);
        }

        //====================================
        // ABM
        //====================================

        public async Task<ServiceResult> CrearAsync(Modelo modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo.Nombre))
                return ServiceResult.Error("Debe ingresar el nombre.");

            if (!await ExisteMarcaAsync(modelo.MarcaId))
                return ServiceResult.Error("La marca seleccionada no existe.");

            if (await ExisteModeloAsync(modelo.Nombre, modelo.MarcaId))
                return ServiceResult.Error("Ya existe un modelo con ese nombre para la marca seleccionada.");

            modelo.Nombre = modelo.Nombre.Trim();
            modelo.FechaCreacion = DateTime.UtcNow;
            modelo.Activo = true;

            _context.Modelos.Add(modelo);

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Modelo creado correctamente.");
        }

        public async Task<ServiceResult> EditarAsync(Modelo modelo)
        {
            var existente = await ObtenerModeloAsync(modelo.Id);

            if (existente == null)
                return ServiceResult.Error("Modelo no encontrado.");

            if (!existente.Activo)
                return ServiceResult.Error("No se puede editar un modelo desactivado.");

            if (!await ExisteMarcaAsync(modelo.MarcaId))
                return ServiceResult.Error("La marca seleccionada no existe.");

            if (await ExisteModeloAsync(modelo.Nombre, modelo.MarcaId, modelo.Id))
                return ServiceResult.Error("Ya existe un modelo con ese nombre para la marca seleccionada.");

            existente.Nombre = modelo.Nombre.Trim();
            existente.MarcaId = modelo.MarcaId;

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Modelo actualizado correctamente.");
        }

        public async Task<ServiceResult> CambiarEstadoAsync(int id)
        {
            var modelo = await ObtenerModeloAsync(id);

            if (modelo == null)
                return ServiceResult.Error("Modelo no encontrado.");

            if (modelo.Activo && modelo.Vehiculos.Any())
                return ServiceResult.Error("No puede desactivarse porque tiene vehículos asociados.");

            modelo.Activo = !modelo.Activo;

            await GuardarCambiosAsync();

            return ServiceResult.Ok(
                modelo.Activo
                    ? "Modelo activado correctamente."
                    : "Modelo desactivado correctamente.");
        }

        //====================================
        // CONSULTAS PRIVADAS
        //====================================

        private async Task<Modelo?> ObtenerModeloAsync(int id)
        {
            return await _context.Modelos
                .Include(m => m.Marca)
                .Include(m => m.Vehiculos)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        private async Task<bool> ExisteMarcaAsync(int marcaId)
        {
            return await _context.Marcas.AnyAsync(m =>
                m.Id == marcaId && m.Activo);
        }

        private async Task<bool> ExisteModeloAsync(
            string nombre,
            int marcaId,
            int? excluirId = null)
        {
            nombre = nombre.Trim().ToUpper();

            return await _context.Modelos.AnyAsync(m =>
                m.MarcaId == marcaId &&
                m.Nombre.ToUpper() == nombre &&
                (!excluirId.HasValue || m.Id != excluirId.Value));
        }

        //====================================
        // OPERACIONES
        //====================================

        private async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}