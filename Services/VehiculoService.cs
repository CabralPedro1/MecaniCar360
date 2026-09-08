using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class VehiculoService
    {
        private readonly MecaniCarContext _context;
        private readonly PermisoService _permisoService;

        public VehiculoService(
            MecaniCarContext context,
            PermisoService permisoService)
        {
            _context = context;
            _permisoService = permisoService;
        }

        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<List<Vehiculo>>> ObtenerTodosAsync(
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "VEHICULO_VER"))
            {
                return ServiceResult<List<Vehiculo>>.Error(
                    "No posee permisos para consultar vehículos.");
            }

            var vehiculos = await _context.Vehiculos
                .Include(v => v.Marca)
                .Include(v => v.Modelo)
                .Include(v => v.DominiosVehiculares)
                    .ThenInclude(d => d.Persona)
                .OrderBy(v => v.Patente)
                .ToListAsync();

            return ServiceResult<List<Vehiculo>>.Ok(vehiculos);
        }

        public async Task<ServiceResult<Vehiculo>> ObtenerPorIdAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "VEHICULO_VER"))
            {
                return ServiceResult<Vehiculo>.Error(
                    "No posee permisos para consultar vehículos.");
            }

            var vehiculo = await ObtenerVehiculoCompletoAsync(id);

            if (vehiculo == null)
                return ServiceResult<Vehiculo>.Error("Vehículo no encontrado.");

            return ServiceResult<Vehiculo>.Ok(vehiculo);
        }

        public async Task<ServiceResult<Vehiculo>> ObtenerPorIdParaEditarAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "VEHICULO_MODIFICAR"))
            {
                return ServiceResult<Vehiculo>.Error(
                    "No posee permisos para modificar vehículos.");
            }

            var vehiculo = await ObtenerVehiculoCompletoAsync(id);

            if (vehiculo == null)
                return ServiceResult<Vehiculo>.Error(
                    "Vehículo no encontrado.");

            return ServiceResult<Vehiculo>.Ok(vehiculo);
        }

        public async Task<ServiceResult<Vehiculo>> ObtenerPorPatenteAsync(
            string patente,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "VEHICULO_VER"))
            {
                return ServiceResult<Vehiculo>.Error(
                    "No posee permisos para consultar vehículos.");
            }

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

        public async Task<ServiceResult<List<Vehiculo>>>
            ObtenerVehiculosPropiosAsync(int usuarioId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioId,
                "CLIENTE_VEHICULO_VER"))
            {
                return ServiceResult<List<Vehiculo>>.Error(
                    "No posee permisos para consultar sus vehículos.");
            }

            var personaId = await ObtenerPersonaIdAsync(usuarioId);

            if (!personaId.HasValue)
            {
                return ServiceResult<List<Vehiculo>>.Error(
                    "Usuario no encontrado o inactivo.");
            }

            var vehiculos = await _context.Vehiculos
                .Include(v => v.Marca)
                .Include(v => v.Modelo)
                .Where(v =>
                    v.Activo &&
                    v.DominiosVehiculares.Any(d =>
                        d.PersonaId == personaId.Value &&
                        d.FechaHasta == null))
                .OrderBy(v => v.Patente)
                .ToListAsync();

            return ServiceResult<List<Vehiculo>>.Ok(vehiculos);
        }

        public async Task<ServiceResult<Vehiculo>>
            ObtenerVehiculoPropioAsync(
                int usuarioId,
                int vehiculoId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioId,
                "CLIENTE_VEHICULO_VER"))
            {
                return ServiceResult<Vehiculo>.Error(
                    "No posee permisos para consultar sus vehículos.");
            }

            var personaId = await ObtenerPersonaIdAsync(usuarioId);

            if (!personaId.HasValue)
            {
                return ServiceResult<Vehiculo>.Error(
                    "Usuario no encontrado o inactivo.");
            }

            var vehiculo = await _context.Vehiculos
                .Include(v => v.Marca)
                .Include(v => v.Modelo)
                .FirstOrDefaultAsync(v =>
                    v.Id == vehiculoId &&
                    v.Activo &&
                    v.DominiosVehiculares.Any(d =>
                        d.PersonaId == personaId.Value &&
                        d.FechaHasta == null));

            if (vehiculo == null)
                return ServiceResult<Vehiculo>.Error(
                    "Vehículo no encontrado.");

            return ServiceResult<Vehiculo>.Ok(vehiculo);
        }

        // =====================================
        // ABM
        // =====================================

        public async Task<ServiceResult> CrearAsync(
            Vehiculo vehiculo,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "VEHICULO_CREAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para crear vehículos.");
            }

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

        public async Task<ServiceResult> EditarAsync(
            Vehiculo vehiculo,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "VEHICULO_MODIFICAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar vehículos.");
            }

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

        public async Task<ServiceResult> CambiarEstadoAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "VEHICULO_MODIFICAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar vehículos.");
            }

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

        private async Task<int?> ObtenerPersonaIdAsync(int usuarioId)
        {
            return await _context.Usuarios
                .Where(u =>
                    u.Id == usuarioId &&
                    u.Activo &&
                    u.Persona.Activo)
                .Select(u => (int?)u.PersonaId)
                .FirstOrDefaultAsync();
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