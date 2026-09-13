using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class MarcaService
    {
        private readonly MecaniCarContext _context;
        private readonly PermisoService _permisos;

        public MarcaService(MecaniCarContext context, PermisoService permisos)
        {
            _context = context;
            _permisos = permisos;
        }

        //====================================
        // CONSULTAS
        //====================================

        public async Task<ServiceResult<List<Marca>>> ObtenerTodosAsync(int usuarioSolicitanteId)
        {
            if (!await _permisos.TieneAlgunoAsync(usuarioSolicitanteId, "VEHICULO_VER", "VEHICULO_CREAR", "VEHICULO_MODIFICAR")) return ServiceResult<List<Marca>>.Error("Acceso denegado.");

            var marcas = await _context.Marcas
                .Include(m => m.Modelos)
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            return ServiceResult<List<Marca>>.Ok(marcas);
        }

        public async Task<ServiceResult<Marca>> ObtenerPorIdAsync(int id, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "VEHICULO_VER")) return ServiceResult<Marca>.Error("Acceso denegado.");

            var marca = await ObtenerMarcaAsync(id);

            if (marca == null)
                return ServiceResult<Marca>.Error("Marca no encontrada.");

            return ServiceResult<Marca>.Ok(marca);
        }

        //====================================
        // ABM
        //====================================

        public async Task<ServiceResult> CrearAsync(Marca marca, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "VEHICULO_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");

            if (string.IsNullOrWhiteSpace(marca.Nombre))
                return ServiceResult.Error("Debe ingresar el nombre.");

            if (await ExisteMarcaAsync(marca.Nombre))
                return ServiceResult.Error("Ya existe una marca con ese nombre.");

            marca.Nombre = marca.Nombre.Trim();
            marca.FechaCreacion = DateTime.UtcNow;
            marca.Activo = true;

            marca.Id = 0; marca.Modelos = new();
            _context.Marcas.Add(marca);

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Marca creada correctamente.");
        }

        public async Task<ServiceResult> EditarAsync(Marca marca, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "VEHICULO_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");

            var existente = await ObtenerMarcaAsync(marca.Id);

            if (existente == null)
                return ServiceResult.Error("Marca no encontrada.");

            if (!existente.Activo)
                return ServiceResult.Error("No se puede editar una marca desactivada.");

            if (await ExisteMarcaAsync(marca.Nombre, marca.Id))
                return ServiceResult.Error("Ya existe una marca con ese nombre.");

            existente.Nombre = marca.Nombre.Trim();

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Marca actualizada correctamente.");
        }

        public async Task<ServiceResult> CambiarEstadoAsync(int id, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "VEHICULO_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");

            var marca = await ObtenerMarcaAsync(id);

            if (marca == null)
                return ServiceResult.Error("Marca no encontrada.");

            if (marca.Activo && marca.Modelos.Any(m => m.Activo))
                return ServiceResult.Error("No puede desactivarse porque posee modelos activos.");

            marca.Activo = !marca.Activo;

            await GuardarCambiosAsync();

            return ServiceResult.Ok(
                marca.Activo
                    ? "Marca activada correctamente."
                    : "Marca desactivada correctamente.");
        }

        //====================================
        // CONSULTAS PRIVADAS
        //====================================

        private async Task<Marca?> ObtenerMarcaAsync(int id)
        {
            return await _context.Marcas
                .Include(m => m.Modelos)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        private async Task<bool> ExisteMarcaAsync(string nombre, int? excluirId = null)
        {
            nombre = nombre.Trim().ToUpper();

            return await _context.Marcas.AnyAsync(m =>
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