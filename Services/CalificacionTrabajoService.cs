using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class CalificacionTrabajoService
    {
        private readonly MecaniCarContext _context;
        private readonly PermisoService _permisos;

        public CalificacionTrabajoService(
            MecaniCarContext context, PermisoService permisos)
        {
            _context = context;
            _permisos = permisos;
        }

        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<CalificacionTrabajo>>
            ObtenerPorOrdenTrabajoAsync(
                int ordenTrabajoId, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "CLIENTE_ORDEN_VER")) return ServiceResult<CalificacionTrabajo>.Error("Acceso denegado.");
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!personaId.HasValue || (!await _permisos.EsAdministradorAsync(usuarioSolicitanteId) &&
                !await _context.OrdenesTrabajo.AnyAsync(o => o.Id == ordenTrabajoId && o.IngresoVehiculo.Turno.ClienteId == personaId)))
                return ServiceResult<CalificacionTrabajo>.Error("Acceso denegado.");

            var calificacion =
                await _context.Calificaciones
                    .Include(c => c.Cliente)
                    .Include(c => c.OrdenTrabajo)
                    .FirstOrDefaultAsync(
                        c => c.OrdenTrabajoId == ordenTrabajoId);

            if (calificacion == null)
            {
                return ServiceResult<CalificacionTrabajo>
                    .Error(
                        "La orden de trabajo no tiene una calificación.");
            }

            return ServiceResult<CalificacionTrabajo>
                .Ok(calificacion);
        }

        public async Task<ServiceResult<List<CalificacionTrabajo>>>
            ObtenerTodasAsync(int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "ORDEN_VER")) return ServiceResult<List<CalificacionTrabajo>>.Error("Acceso denegado.");
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            var limitarAlMecanico = !await _permisos.EsAdministradorAsync(usuarioSolicitanteId) &&
                await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == personaId &&
                    pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.MECANICO);

            var calificaciones =
                await _context.Calificaciones
                    .Include(c => c.Cliente)
                    .Include(c => c.OrdenTrabajo)
                    .Where(c => !limitarAlMecanico || c.OrdenTrabajo.MecanicoId == personaId)
                    .OrderByDescending(c => c.Fecha)
                    .ToListAsync();

            return ServiceResult<List<CalificacionTrabajo>>
                .Ok(calificaciones);
        }

        // =====================================
        // ABM
        // =====================================

        public async Task<ServiceResult> CrearAsync(
            int ordenTrabajoId,
            int usuarioSolicitanteId,
            int puntuacion,
            string? comentario)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "CLIENTE_CALIFICACION_CREAR")) return ServiceResult.Error("Acceso denegado.");
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!personaId.HasValue) return ServiceResult.Error("Acceso denegado.");
            var clienteId = personaId.Value;
            if (!await _context.OrdenesTrabajo.AnyAsync(o => o.Id == ordenTrabajoId && o.IngresoVehiculo.Turno.ClienteId == clienteId))
                return ServiceResult.Error("La orden no pertenece al cliente autenticado.");

            if (puntuacion < 1 || puntuacion > 5)
            {
                return ServiceResult.Error(
                    "La puntuación debe estar entre 1 y 5.");
            }

            var orden =
                await _context.OrdenesTrabajo
                    .Include(o => o.IngresoVehiculo)
                        .ThenInclude(i => i.Turno)
                            .ThenInclude(t => t.Vehiculo)
                            .ThenInclude(v => v.DominiosVehiculares)
                    .FirstOrDefaultAsync(
                        o => o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult.Error(
                    "La orden de trabajo no existe.");
            }

            // La calificación se habilita únicamente
            // después de entregar el vehículo.
            if (orden.EstadoActual != EstadoOrden.Entregado)
            {
                return ServiceResult.Error(
                    "Solo se puede calificar una orden de trabajo entregada.");
            }

            // Una sola calificación por orden.
            var yaExiste =
                await _context.Calificaciones
                    .AnyAsync(
                        c => c.OrdenTrabajoId == ordenTrabajoId);

            if (yaExiste)
            {
                return ServiceResult.Error(
                    "Esta orden de trabajo ya fue calificada.");
            }

            // El cliente debe ser el titular actual
            // del vehículo correspondiente a la orden.
            var clienteEsPropietario =
                orden.IngresoVehiculo.Turno.Vehiculo.DominiosVehiculares
                    .Any(d =>
                        d.PersonaId == clienteId &&
                        d.FechaHasta == null);

            if (!clienteEsPropietario)
            {
                return ServiceResult.Error(
                    "El cliente no puede calificar esta orden de trabajo.");
            }

            var calificacion =
                new CalificacionTrabajo
                {
                    OrdenTrabajoId = ordenTrabajoId,
                    ClienteId = clienteId,
                    Puntuacion = puntuacion,
                    Comentario =
                        string.IsNullOrWhiteSpace(comentario)
                            ? null
                            : comentario.Trim(),
                    Fecha = DateTime.Now
                };

            _context.Calificaciones.Add(calificacion);

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Calificación registrada correctamente.");
        }
    }
}
