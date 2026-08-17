using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Patterns.State;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class OrdenTrabajoService
    {
        private readonly MecaniCarContext _context;
        private readonly OrdenStateService _ordenStateService;

        public OrdenTrabajoService(
            MecaniCarContext context,
            OrdenStateService ordenStateService)
        {
            _context = context;
            _ordenStateService = ordenStateService;
        }


        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<List<OrdenTrabajo>>>
            ObtenerTodasAsync()
        {
            var ordenes =
                await _context.OrdenesTrabajo
                    .Include(o => o.Turno)
                        .ThenInclude(t => t.Vehiculo)
                            .ThenInclude(v => v.Marca)
                    .Include(o => o.Turno)
                        .ThenInclude(t => t.Vehiculo)
                            .ThenInclude(v => v.Modelo)
                    .Include(o => o.Turno)
                        .ThenInclude(t => t.Cliente)
                    .Include(o => o.Mecanico)
                    .OrderByDescending(o => o.FechaInicio)
                    .ToListAsync();

            return ServiceResult<List<OrdenTrabajo>>.Ok(
                ordenes);
        }


        public async Task<ServiceResult<OrdenTrabajo>>
            ObtenerPorIdAsync(int id)
        {
            var orden =
                await ObtenerOrdenCompletaAsync(id);

            if (orden == null)
            {
                return ServiceResult<OrdenTrabajo>.Error(
                    "Orden de trabajo no encontrada.");
            }

            return ServiceResult<OrdenTrabajo>.Ok(
                orden);
        }


        public async Task<ServiceResult<List<OrdenTrabajo>>>
            ObtenerPendientesAsync()
        {
            var ordenes =
                await _context.OrdenesTrabajo
                    .Include(o => o.Turno)
                        .ThenInclude(t => t.Vehiculo)
                    .Include(o => o.Turno)
                        .ThenInclude(t => t.Cliente)
                    .Where(o =>
                        o.EstadoActual ==
                            EstadoOrden.Pendiente &&
                        o.FechaFin == null)
                    .OrderBy(o => o.Urgencia)
                    .ThenBy(o => o.FechaInicio)
                    .ToListAsync();

            return ServiceResult<List<OrdenTrabajo>>.Ok(
                ordenes);
        }


        public async Task<ServiceResult<List<OrdenTrabajo>>>
            ObtenerDeMecanicoAsync(int mecanicoId)
        {
            var ordenes =
                await _context.OrdenesTrabajo
                    .Include(o => o.Turno)
                        .ThenInclude(t => t.Vehiculo)
                    .Include(o => o.Turno)
                        .ThenInclude(t => t.Cliente)
                    .Where(o =>
                        o.MecanicoId == mecanicoId &&
                        o.FechaFin == null)
                    .OrderBy(o => o.FechaInicio)
                    .ToListAsync();

            return ServiceResult<List<OrdenTrabajo>>.Ok(
                ordenes);
        }


        // =====================================
        // MECÁNICOS
        // =====================================

        public async Task<ServiceResult<List<Persona>>>
            ObtenerMecanicosDisponiblesAsync()
        {
            var mecanicos =
                await _context.PersonaRoles
                    .Include(pr => pr.Persona)
                    .Include(pr => pr.Rol)
                    .Where(pr =>
                        pr.Persona.Activo &&
                        pr.Rol.Nombre == "MECANICO" &&
                        pr.Rol.Activo &&
                        pr.FechaBaja == null)
                    .Select(pr => pr.Persona)
                    .Distinct()
                    .OrderBy(p => p.Apellido)
                    .ThenBy(p => p.Nombre)
                    .ToListAsync();

            return ServiceResult<List<Persona>>.Ok(
                mecanicos);
        }


        public async Task<ServiceResult>
            AsignarMecanicoAsync(
                int ordenTrabajoId,
                int mecanicoId)
        {
            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult.Error(
                    "Orden de trabajo no encontrada.");
            }

            if (orden.FechaFin.HasValue)
            {
                return ServiceResult.Error(
                    "La orden de trabajo ya finalizó.");
            }

            if (!await EsMecanicoActivoAsync(mecanicoId))
            {
                return ServiceResult.Error(
                    "La persona seleccionada no es un mecánico activo.");
            }

            if (orden.MecanicoId.HasValue &&
                orden.MecanicoId.Value != mecanicoId)
            {
                return ServiceResult.Error(
                    "La orden ya está asignada a otro mecánico.");
            }

            orden.MecanicoId = mecanicoId;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Mecánico asignado correctamente.");
        }


        public async Task<ServiceResult>
            TomarOrdenAsync(
                int ordenTrabajoId,
                int mecanicoId)
        {
            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult.Error(
                    "Orden de trabajo no encontrada.");
            }

            if (orden.FechaFin.HasValue)
            {
                return ServiceResult.Error(
                    "La orden ya fue finalizada.");
            }

            if (orden.MecanicoId.HasValue &&
                orden.MecanicoId.Value != mecanicoId)
            {
                return ServiceResult.Error(
                    "La orden ya fue tomada por otro mecánico.");
            }

            if (!await EsMecanicoActivoAsync(mecanicoId))
            {
                return ServiceResult.Error(
                    "La persona seleccionada no es un mecánico activo.");
            }

            orden.MecanicoId = mecanicoId;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Orden tomada correctamente.");
        }


        // =====================================
        // INICIAR REPARACIÓN
        // =====================================

        public async Task<ServiceResult>
            IniciarReparacionAsync(
                int ordenTrabajoId,
                int mecanicoId)
        {
            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult.Error(
                    "Orden de trabajo no encontrada.");
            }

            if (orden.FechaFin.HasValue)
            {
                return ServiceResult.Error(
                    "La orden ya finalizó.");
            }

            if (orden.MecanicoId != mecanicoId)
            {
                return ServiceResult.Error(
                    "La orden no está asignada a este mecánico.");
            }

            if (orden.EstadoActual !=
                EstadoOrden.Aprobado)
            {
                return ServiceResult.Error(
                    "La orden debe estar aprobada para iniciar la reparación.");
            }

            // =====================================
            // STATE PATTERN
            // =====================================

            _ordenStateService.CambiarEstado(
                orden,
                new EstadoEnReparacionHandler());

            var historial =
                orden.HistorialEstados.LastOrDefault();

            if (historial != null)
            {
                historial.MecanicoId = mecanicoId;
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Reparación iniciada correctamente.");
        }


        // =====================================
        // FINALIZAR REPARACIÓN
        // =====================================

        public async Task<ServiceResult>
            FinalizarAsync(
                int ordenTrabajoId,
                int mecanicoId,
                int? horasReales = null)
        {
            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult.Error(
                    "Orden de trabajo no encontrada.");
            }

            if (orden.FechaFin.HasValue)
            {
                return ServiceResult.Error(
                    "La orden ya está finalizada.");
            }

            if (orden.MecanicoId != mecanicoId)
            {
                return ServiceResult.Error(
                    "La orden no está asignada a este mecánico.");
            }

            if (orden.EstadoActual !=
                EstadoOrden.EnReparacion)
            {
                return ServiceResult.Error(
                    "La orden debe estar en reparación para finalizarla.");
            }

            if (horasReales.HasValue &&
                (horasReales.Value < 0 ||
                 horasReales.Value > 1000))
            {
                return ServiceResult.Error(
                    "Las horas reales no son válidas.");
            }

            // =====================================
            // STATE PATTERN
            // =====================================

            _ordenStateService.CambiarEstado(
                orden,
                new EstadoFinalizadoHandler());

            orden.FechaFin =
                DateTime.Now;

            orden.HorasReales =
                horasReales;

            var historial =
                orden.HistorialEstados.LastOrDefault();

            if (historial != null)
            {
                historial.MecanicoId = mecanicoId;
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Orden de trabajo finalizada correctamente.");
        }


        // =====================================
        // ENTREGAR VEHÍCULO
        // =====================================

        public async Task<ServiceResult>
            EntregarAsync(
                int ordenTrabajoId)
        {
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var orden =
                    await _context.OrdenesTrabajo
                        .Include(o => o.Factura)
                            .ThenInclude(f => f!.Pagos)
                        .Include(o => o.Turno)
                            .ThenInclude(t => t.IngresoVehiculo)
                        .FirstOrDefaultAsync(o =>
                            o.Id == ordenTrabajoId);

                if (orden == null)
                {
                    return ServiceResult.Error(
                        "Orden de trabajo no encontrada.");
                }

                if (orden.EstadoActual ==
                    EstadoOrden.Entregado)
                {
                    return ServiceResult.Error(
                        "El vehículo ya fue entregado.");
                }

                if (orden.EstadoActual !=
                        EstadoOrden.Finalizado &&
                    orden.EstadoActual !=
                        EstadoOrden.Rechazado)
                {
                    return ServiceResult.Error(
                        "La orden debe estar finalizada o rechazada antes de entregar el vehículo.");
                }

                // =====================================
                // FACTURA
                // =====================================

                if (orden.Factura == null)
                {
                    return ServiceResult.Error(
                        "No se puede entregar el vehículo porque todavía no existe una factura.");
                }

                if (orden.Factura.Estado !=
                    EstadoFactura.Pagada)
                {
                    return ServiceResult.Error(
                        "No se puede entregar el vehículo porque la factura todavía no está pagada.");
                }

                // =====================================
                // PAGO
                // =====================================

                var totalPagado =
                    orden.Factura.Pagos
                        .Where(p =>
                            p.Estado ==
                            EstadoPago.Pagado)
                        .Sum(p =>
                            p.Monto);

                if (totalPagado <
                    orden.Factura.Total)
                {
                    return ServiceResult.Error(
                        "No se puede entregar el vehículo porque el pago no cubre el total de la factura.");
                }

                // =====================================
                // INGRESO
                // =====================================

                var ingreso =
                    orden.Turno?.IngresoVehiculo;

                if (ingreso == null)
                {
                    return ServiceResult.Error(
                        "No se encontró el ingreso del vehículo asociado a la orden.");
                }

                if (ingreso.FechaEgreso.HasValue)
                {
                    return ServiceResult.Error(
                        "El vehículo ya posee una fecha de egreso.");
                }

                // =====================================
                // STATE PATTERN
                // =====================================

                _ordenStateService.CambiarEstado(
                    orden,
                    new EstadoEntregadoHandler());

                ingreso.FechaEgreso =
                    DateTime.Now;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return ServiceResult.Ok(
                    "Vehículo entregado correctamente.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // =====================================
        // CAMBIAR URGENCIA
        // =====================================

        public async Task<ServiceResult>
            CambiarUrgenciaAsync(
                int ordenTrabajoId,
                NivelUrgencia urgencia)
        {
            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult.Error(
                    "Orden de trabajo no encontrada.");
            }

            if (orden.EstadoActual ==
                EstadoOrden.Entregado)
            {
                return ServiceResult.Error(
                    "No se puede modificar una orden entregada.");
            }

            orden.Urgencia =
                urgencia;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Nivel de urgencia actualizado.");
        }


        // =====================================
        // OBSERVACIONES
        // =====================================

        public async Task<ServiceResult>
            ActualizarObservacionesAsync(
                int ordenTrabajoId,
                string? observaciones)
        {
            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult.Error(
                    "Orden de trabajo no encontrada.");
            }

            if (orden.EstadoActual ==
                EstadoOrden.Entregado)
            {
                return ServiceResult.Error(
                    "No se pueden modificar las observaciones de una orden entregada.");
            }

            orden.Observaciones =
                string.IsNullOrWhiteSpace(observaciones)
                    ? null
                    : observaciones.Trim();

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Observaciones actualizadas correctamente.");
        }


        // =====================================
        // MÉTODOS PRIVADOS
        // =====================================

        private async Task<bool>
            EsMecanicoActivoAsync(
                int personaId)
        {
            return await _context.PersonaRoles
                .Include(pr => pr.Rol)
                .Include(pr => pr.Persona)
                .AnyAsync(pr =>
                    pr.PersonaId == personaId &&
                    pr.Persona.Activo &&
                    pr.Rol.Nombre == "MECANICO" &&
                    pr.Rol.Activo &&
                    pr.FechaBaja == null);
        }


        private async Task<OrdenTrabajo?>
            ObtenerOrdenCompletaAsync(
                int id)
        {
            return await _context.OrdenesTrabajo
                .Include(o => o.Turno)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Marca)

                .Include(o => o.Turno)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Modelo)

                .Include(o => o.Turno)
                    .ThenInclude(t => t.Cliente)

                .Include(o => o.Mecanico)

                .Include(o => o.Diagnosticos)
                    .ThenInclude(d => d.Historial)
                        .ThenInclude(h => h.Mecanico)

                .Include(o => o.Presupuesto)
                    .ThenInclude(p => p!.Items)

                .Include(o => o.Factura)
                    .ThenInclude(f => f!.Pagos)

                .Include(o => o.HistorialEstados)
                    .ThenInclude(h => h.Mecanico)

                .Include(o => o.Especialidades)
                    .ThenInclude(e => e.Especialidad)

                .Include(o => o.Evidencias)

                .FirstOrDefaultAsync(o =>
                    o.Id == id);
        }
    }
}