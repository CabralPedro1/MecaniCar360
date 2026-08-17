using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class VehiculoService
    {
        private readonly MecaniCarContext _context;

        public VehiculoService(MecaniCarContext context)
        {
            _context = context;
        }

        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<List<Vehiculo>>> ObtenerTodosAsync()
        {
            var vehiculos = await _context.Vehiculos
                .Include(v => v.Marca)
                .Include(v => v.Modelo)
                .Include(v => v.DominiosVehiculares)
                    .ThenInclude(d => d.Persona)
                .OrderBy(v => v.Patente)
                .ToListAsync();

            return ServiceResult<List<Vehiculo>>.Ok(vehiculos);
        }

        public async Task<ServiceResult<Vehiculo>> ObtenerPorIdAsync(int id)
        {
            var vehiculo = await ObtenerVehiculoCompletoAsync(id);

            if (vehiculo == null)
                return ServiceResult<Vehiculo>.Error("Vehículo no encontrado.");

            return ServiceResult<Vehiculo>.Ok(vehiculo);
        }

        public async Task<ServiceResult<Vehiculo>> ObtenerPorPatenteAsync(string patente)
        {
            patente = patente.Trim().ToUpper();

            var vehiculo = await _context.Vehiculos
                .Include(v => v.Marca)
                .Include(v => v.Modelo)
                .Include(v => v.DominiosVehiculares)
                    .ThenInclude(d => d.Persona)
                .FirstOrDefaultAsync(v => v.Patente == patente);

            if (vehiculo == null)
                return ServiceResult<Vehiculo>.Error("Vehículo no encontrado.");

            return ServiceResult<Vehiculo>.Ok(vehiculo);
        }

        public async Task<ServiceResult<List<Modelo>>> ObtenerModelosPorMarcaAsync(int marcaId)
        {
            var modelos = await _context.Modelos
                .Where(m => m.MarcaId == marcaId && m.Activo)
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            return ServiceResult<List<Modelo>>.Ok(modelos);
        }

        // =====================================
        // ABM
        // =====================================

        public async Task<ServiceResult> CrearAsync(Vehiculo vehiculo)
        {
            var validacion = await ValidarVehiculoAsync(vehiculo);

            if (!validacion.Exitoso)
                return validacion;

            vehiculo.Patente = vehiculo.Patente.Trim().ToUpper();

            if (!string.IsNullOrWhiteSpace(vehiculo.Vin))
                vehiculo.Vin = vehiculo.Vin.Trim().ToUpper();

            vehiculo.FechaCreacion = DateTime.Now;
            vehiculo.Activo = true;

            _context.Vehiculos.Add(vehiculo);

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Vehículo creado correctamente.");
        }

        public async Task<ServiceResult> EditarAsync(Vehiculo vehiculo)
        {
            var existente = await _context.Vehiculos.FindAsync(vehiculo.Id);

            if (existente == null)
                return ServiceResult.Error("Vehículo no encontrado.");

            if (!existente.Activo)
                return ServiceResult.Error("No se puede editar un vehículo desactivado.");

            var validacion = await ValidarVehiculoAsync(vehiculo, vehiculo.Id);

            if (!validacion.Exitoso)
                return validacion;

            existente.Patente = vehiculo.Patente.Trim().ToUpper();

            existente.Vin = string.IsNullOrWhiteSpace(vehiculo.Vin)
                ? null
                : vehiculo.Vin.Trim().ToUpper();

            existente.MarcaId = vehiculo.MarcaId;
            existente.ModeloId = vehiculo.ModeloId;
            existente.Anio = vehiculo.Anio;
            existente.Color = vehiculo.Color;
            existente.Motor = vehiculo.Motor;
            existente.Version = vehiculo.Version;
            existente.Kilometraje = vehiculo.Kilometraje;

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Vehículo actualizado correctamente.");
        }

        public async Task<ServiceResult> CambiarEstadoAsync(int id)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(id);

            if (vehiculo == null)
                return ServiceResult.Error("Vehículo no encontrado.");

            vehiculo.Activo = !vehiculo.Activo;

            await GuardarCambiosAsync();

            return ServiceResult.Ok(
                vehiculo.Activo
                    ? "Vehículo activado correctamente."
                    : "Vehículo desactivado correctamente.");
        }


        // =====================================
        // MÉTODOS PRIVADOS
        // =====================================

        private async Task<Vehiculo?> ObtenerVehiculoCompletoAsync(int id)
        {
            return await _context.Vehiculos
                .Include(v => v.Marca)
                .Include(v => v.Modelo)
                .Include(v => v.DominiosVehiculares)
                    .ThenInclude(d => d.Persona)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        private async Task<bool> ExistePatenteAsync(
            string patente,
            int? excluirId = null)
        {
            patente = patente.Trim().ToUpper();

            return await _context.Vehiculos.AnyAsync(v =>
                v.Patente == patente &&
                (!excluirId.HasValue || v.Id != excluirId.Value));
        }

        private async Task<bool> ExisteVinAsync(
            string? vin,
            int? excluirId = null)
        {
            if (string.IsNullOrWhiteSpace(vin))
                return false;

            vin = vin.Trim().ToUpper();

            return await _context.Vehiculos.AnyAsync(v =>
                v.Vin == vin &&
                (!excluirId.HasValue || v.Id != excluirId.Value));
        }

        private async Task<ServiceResult> ValidarVehiculoAsync(
            Vehiculo vehiculo,
            int? excluirId = null)
        {
            if (string.IsNullOrWhiteSpace(vehiculo.Patente))
                return ServiceResult.Error("Debe ingresar la patente.");

            if (await ExistePatenteAsync(vehiculo.Patente, excluirId))
                return ServiceResult.Error("Ya existe un vehículo con esa patente.");

            if (await ExisteVinAsync(vehiculo.Vin, excluirId))
                return ServiceResult.Error("Ya existe un vehículo con ese VIN.");

            var marca = await _context.Marcas
                .FirstOrDefaultAsync(m => m.Id == vehiculo.MarcaId && m.Activo);

            if (marca == null)
                return ServiceResult.Error("La marca seleccionada no existe.");

            var modelo = await _context.Modelos
                .FirstOrDefaultAsync(m =>
                    m.Id == vehiculo.ModeloId &&
                    m.Activo);

            if (modelo == null)
                return ServiceResult.Error("El modelo seleccionado no existe.");

            if (modelo.MarcaId != vehiculo.MarcaId)
                return ServiceResult.Error("El modelo no pertenece a la marca seleccionada.");

            return ServiceResult.Ok();
        }

        private async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }

    }
}