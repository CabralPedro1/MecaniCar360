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


        // =====================================================
        // CONSULTAS
        // =====================================================

        // -----------------------------------------------------
        // TODAS LAS ÓRDENES
        // Solo ADMIN
        // -----------------------------------------------------

        public async Task<ServiceResult<List<OrdenTrabajo>>>
            ObtenerTodasAsync(int personaId)
        {
            if (!await EsAdministradorAsync(personaId))
            {
                return ServiceResult<List<OrdenTrabajo>>.Error(
                    "No tiene permisos para consultar todas las órdenes.");
            }

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


        // -----------------------------------------------------
        // OBTENER UNA ORDEN
        // ADMIN puede ver cualquiera.
        // MECÁNICO solo las asignadas a él.
        // -----------------------------------------------------

        public async Task<ServiceResult<OrdenTrabajo>>
            ObtenerPorIdAsync(
                int id,
                int personaId)
        {
            var orden =
                await ObtenerOrdenCompletaAsync(id);

            if (orden == null)
            {
                return ServiceResult<OrdenTrabajo>.Error(
                    "Orden de trabajo no encontrada.");
            }

            bool esAdmin =
                await EsAdministradorAsync(personaId);

            bool esMecanico =
                await EsMecanicoActivoAsync(personaId);

            if (esAdmin)
            {
                return ServiceResult<OrdenTrabajo>.Ok(
                    orden);
            }

            if (esMecanico)
            {
                if (orden.MecanicoId != personaId)
                {
                    return ServiceResult<OrdenTrabajo>.Error(
                        "No tiene acceso a esta orden de trabajo.");
                }

                return ServiceResult<OrdenTrabajo>.Ok(
                    orden);
            }

            return ServiceResult<OrdenTrabajo>.Error(
                "No tiene permisos para consultar esta orden.");
        }


        // -----------------------------------------------------
        // ÓRDENES PENDIENTES DISPONIBLES
        //
        // Pensado para que el MECÁNICO pueda ver
        // órdenes que todavía puede tomar.
        // -----------------------------------------------------

        public async Task<ServiceResult<List<OrdenTrabajo>>>
            ObtenerPendientesAsync(int personaId)
        {
            if (!await EsMecanicoActivoAsync(personaId) &&
                !await EsAdministradorAsync(personaId))
            {
                return ServiceResult<List<OrdenTrabajo>>.Error(
                    "No tiene permisos para consultar las órdenes pendientes.");
            }

            var ordenes =
                await _context.OrdenesTrabajo

                    .Include(o => o.Turno)
                        .ThenInclude(t => t.Vehiculo)

                    .Include(o => o.Turno)
                        .ThenInclude(t => t.Cliente)

                    .Where(o =>
                        o.EstadoActual ==
                            EstadoOrden.Pendiente &&

                        o.FechaFin == null &&

                        o.MecanicoId == null)

                    .OrderBy(o => o.Urgencia)
                    .ThenBy(o => o.FechaInicio)

                    .ToListAsync();

            return ServiceResult<List<OrdenTrabajo>>.Ok(
                ordenes);
        }


        // -----------------------------------------------------
        // ÓRDENES DE UN MECÁNICO
        //
        // ADMIN puede consultar cualquier mecánico.
        // MECÁNICO solo puede consultar sus propias órdenes.
        // -----------------------------------------------------

        public async Task<ServiceResult<List<OrdenTrabajo>>>
            ObtenerDeMecanicoAsync(
                int mecanicoId,
                int personaSolicitanteId)
        {
            bool esAdmin =
                await EsAdministradorAsync(
                    personaSolicitanteId);

            bool esMecanicoSolicitante =
                await EsMecanicoActivoAsync(
                    personaSolicitanteId);

            if (!esAdmin)
            {
                if (!esMecanicoSolicitante ||
                    mecanicoId != personaSolicitanteId)
                {
                    return ServiceResult<List<OrdenTrabajo>>.Error(
                        "No tiene acceso a las órdenes de este mecánico.");
                }
            }

            if (!await EsMecanicoActivoAsync(mecanicoId))
            {
                return ServiceResult<List<OrdenTrabajo>>.Error(
                    "El mecánico indicado no está activo.");
            }

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


        // =====================================================
        // MECÁNICOS
        // =====================================================

        public async Task<ServiceResult<List<Persona>>>
            ObtenerMecanicosDisponiblesAsync(
                int personaSolicitanteId)
        {
            if (!await EsAdministradorAsync(
                    personaSolicitanteId))
            {
                return ServiceResult<List<Persona>>.Error(
                    "Solo un administrador puede consultar los mecánicos para asignación.");
            }

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


        // =====================================================
        // ASIGNAR MECÁNICO
        // SOLO ADMIN
        // =====================================================

        public async Task<ServiceResult>
            AsignarMecanicoAsync(
                int ordenTrabajoId,
                int mecanicoId,
                int personaSolicitanteId)
        {
            if (!await EsAdministradorAsync(
                    personaSolicitanteId))
            {
                return ServiceResult.Error(
                    "Solo un administrador puede asignar mecánicos.");
            }

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

            if (!await EsMecanicoActivoAsync(
                    mecanicoId))
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

            orden.MecanicoId =
                mecanicoId;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Mecánico asignado correctamente.");
        }


        // =====================================================
        // TOMAR ORDEN
        // MECÁNICO
        // =====================================================

        public async Task<ServiceResult>
            TomarOrdenAsync(
                int ordenTrabajoId,
                int mecanicoId)
        {
            if (!await EsMecanicoActivoAsync(
                    mecanicoId))
            {
                return ServiceResult.Error(
                    "La persona seleccionada no es un mecánico activo.");
            }

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

            if (orden.MecanicoId.HasValue)
            {
                if (orden.MecanicoId.Value ==
                    mecanicoId)
                {
                    return ServiceResult.Ok(
                        "La orden ya está asignada a este mecánico.");
                }

                return ServiceResult.Error(
                    "La orden ya fue tomada por otro mecánico.");
            }

            if (orden.EstadoActual !=
                EstadoOrden.Pendiente)
            {
                return ServiceResult.Error(
                    "La orden no se encuentra disponible para ser tomada.");
            }

            orden.MecanicoId =
                mecanicoId;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Orden tomada correctamente.");
        }


        // =====================================================
        // INICIAR REPARACIÓN
        // MECÁNICO
        // =====================================================

        public async Task<ServiceResult>
            IniciarReparacionAsync(
                int ordenTrabajoId,
                int mecanicoId)
        {
            if (!await EsMecanicoActivoAsync(
                    mecanicoId))
            {
                return ServiceResult.Error(
                    "El mecánico no está activo.");
            }

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

            if (orden.MecanicoId !=
                mecanicoId)
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
                historial.MecanicoId =
                    mecanicoId;
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Reparación iniciada correctamente.");
        }


        // =====================================================
        // FINALIZAR REPARACIÓN
        // MECÁNICO
        // =====================================================

        public async Task<ServiceResult>
            FinalizarAsync(
                int ordenTrabajoId,
                int mecanicoId,
                int? horasReales = null)
        {
            if (!await EsMecanicoActivoAsync(
                    mecanicoId))
            {
                return ServiceResult.Error(
                    "El mecánico no está activo.");
            }

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

            if (orden.MecanicoId !=
                mecanicoId)
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
                historial.MecanicoId =
                    mecanicoId;
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Orden de trabajo finalizada correctamente.");
        }


        // =====================================================
        // ENTREGAR VEHÍCULO
        //
        // ADMIN o CAJA
        //
        // La autorización del controller deberá utilizar
        // ORDEN_ENTREGAR.
        // =====================================================

        public async Task<ServiceResult>
            EntregarAsync(
                int ordenTrabajoId,
                int personaSolicitanteId)
        {
            bool esAdmin =
                await EsAdministradorAsync(
                    personaSolicitanteId);

            bool esCaja =
                await EsCajaAsync(
                    personaSolicitanteId);

            if (!esAdmin && !esCaja)
            {
                return ServiceResult.Error(
                    "No tiene permisos para entregar vehículos.");
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var orden =
                    await _context.OrdenesTrabajo

                        .Include(o => o.Factura)
                            .ThenInclude(f =>
                                f!.Pagos)

                        .Include(o => o.Turno)
                            .ThenInclude(t =>
                                t.IngresoVehiculo)

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


        // =====================================================
        // CAMBIAR URGENCIA
        //
        // ADMIN o MECÁNICO ASIGNADO
        // =====================================================

        public async Task<ServiceResult>
            CambiarUrgenciaAsync(
                int ordenTrabajoId,
                NivelUrgencia urgencia,
                int personaSolicitanteId)
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

            bool esAdmin =
                await EsAdministradorAsync(
                    personaSolicitanteId);

            bool esMecanicoAsignado =
                await EsMecanicoActivoAsync(
                    personaSolicitanteId) &&
                orden.MecanicoId ==
                    personaSolicitanteId;

            if (!esAdmin &&
                !esMecanicoAsignado)
            {
                return ServiceResult.Error(
                    "No tiene permisos para modificar esta orden.");
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


        // =====================================================
        // OBSERVACIONES
        //
        // ADMIN o MECÁNICO ASIGNADO
        // =====================================================

        public async Task<ServiceResult>
            ActualizarObservacionesAsync(
                int ordenTrabajoId,
                string? observaciones,
                int personaSolicitanteId)
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

            bool esAdmin =
                await EsAdministradorAsync(
                    personaSolicitanteId);

            bool esMecanicoAsignado =
                await EsMecanicoActivoAsync(
                    personaSolicitanteId) &&
                orden.MecanicoId ==
                    personaSolicitanteId;

            if (!esAdmin &&
                !esMecanicoAsignado)
            {
                return ServiceResult.Error(
                    "No tiene permisos para modificar esta orden.");
            }

            if (orden.EstadoActual ==
                EstadoOrden.Entregado)
            {
                return ServiceResult.Error(
                    "No se pueden modificar las observaciones de una orden entregada.");
            }

            orden.Observaciones =
                string.IsNullOrWhiteSpace(
                    observaciones)
                    ? null
                    : observaciones.Trim();

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Observaciones actualizadas correctamente.");
        }


        // =====================================================
        // MÉTODOS PRIVADOS
        // =====================================================

        private async Task<bool>
            EsAdministradorAsync(
                int personaId)
        {
            return await _context.PersonaRoles
                .Include(pr => pr.Rol)
                .AnyAsync(pr =>
                    pr.PersonaId == personaId &&
                    pr.Rol.Nombre ==
                        "ADMIN" &&
                    pr.Rol.Activo &&
                    pr.FechaBaja == null);
        }


        private async Task<bool>
            EsCajaAsync(
                int personaId)
        {
            return await _context.PersonaRoles
                .Include(pr => pr.Rol)
                .AnyAsync(pr =>
                    pr.PersonaId == personaId &&
                    pr.Rol.Nombre ==
                        "CAJA" &&
                    pr.Rol.Activo &&
                    pr.FechaBaja == null);
        }


        private async Task<bool>
            EsMecanicoActivoAsync(
                int personaId)
        {
            return await _context.PersonaRoles

                .Include(pr => pr.Rol)
                .Include(pr => pr.Persona)

                .AnyAsync(pr =>
                    pr.PersonaId ==
                        personaId &&

                    pr.Persona.Activo &&

                    pr.Rol.Nombre ==
                        "MECANICO" &&

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