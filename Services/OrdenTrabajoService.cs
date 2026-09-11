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
        private readonly PermisoService _permisoService;

        public OrdenTrabajoService(
            MecaniCarContext context,
            OrdenStateService ordenStateService,
            PermisoService permisoService)
        {
            _context = context;
            _ordenStateService = ordenStateService;
            _permisoService = permisoService;
        }


        // =====================================================
        // CONSULTAS
        // =====================================================

        // -----------------------------------------------------
        // TODAS LAS ÓRDENES
        // ORDEN_VER; el mec?nico no administrador s?lo ve sus asignadas.
        // -----------------------------------------------------

        public async Task<ServiceResult<List<OrdenTrabajo>>>
            ObtenerTodasAsync(int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAutorizadoAsync(
                usuarioSolicitanteId, "ORDEN_VER");

            if (usuario == null)
                return ServiceResult<List<OrdenTrabajo>>.Error(
                    "Usuario inactivo o sin permisos para esta operación.");

            var personaId = usuario.PersonaId;

            var limitarAlMecanico =
                !await _permisoService.EsAdministradorAsync(usuarioSolicitanteId) &&
                await EsMecanicoActivoAsync(personaId);

            var ordenes =
                await _context.OrdenesTrabajo
                    .Include(o => o.IngresoVehiculo)
                        .ThenInclude(i => i.Turno)
                            .ThenInclude(t => t.Vehiculo)
                            .ThenInclude(v => v.Marca)

                    .Include(o => o.IngresoVehiculo)
                        .ThenInclude(i => i.Turno)
                            .ThenInclude(t => t.Vehiculo)
                            .ThenInclude(v => v.Modelo)

                    .Include(o => o.IngresoVehiculo)
                        .ThenInclude(i => i.Turno)
                            .ThenInclude(t => t.Cliente)

                    .Include(o => o.Mecanico)

                    .Where(o => !limitarAlMecanico || o.MecanicoId == personaId)
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
                int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAutorizadoAsync(
                usuarioSolicitanteId, "ORDEN_VER_DETALLE");

            if (usuario == null)
                return ServiceResult<OrdenTrabajo>.Error(
                    "Usuario inactivo o sin permisos para esta operación.");

            var personaId = usuario.PersonaId;

            var orden =
                await ObtenerOrdenCompletaAsync(id);

            if (orden == null)
            {
                return ServiceResult<OrdenTrabajo>.Error(
                    "Orden de trabajo no encontrada.");
            }

            bool esAdmin =
                await _permisoService.EsAdministradorAsync(usuarioSolicitanteId);

            bool esMecanico =
                await EsMecanicoActivoAsync(personaId);

            if (esAdmin)
            {
                return ServiceResult<OrdenTrabajo>.Ok(
                    orden);
            }

            if (esMecanico)
            {
                if (orden.MecanicoId != personaId &&
                    !(orden.MecanicoId == null &&
                      orden.EstadoActual == EstadoOrden.Pendiente &&
                      !orden.FechaFin.HasValue))
                {
                    return ServiceResult<OrdenTrabajo>.Error(
                        "No tiene acceso a esta orden de trabajo.");
                }

                return ServiceResult<OrdenTrabajo>.Ok(
                    orden);
            }

            return ServiceResult<OrdenTrabajo>.Ok(orden);
        }


        // -----------------------------------------------------
        // ÓRDENES PENDIENTES DISPONIBLES
        //
        // Pensado para que el MECÁNICO pueda ver
        // órdenes que todavía puede tomar.
        // -----------------------------------------------------

        public async Task<ServiceResult<List<OrdenTrabajo>>>
            ObtenerPendientesAsync(int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAutorizadoAsync(
                usuarioSolicitanteId, "ORDEN_VER");

            if (usuario == null)
                return ServiceResult<List<OrdenTrabajo>>.Error(
                    "Usuario inactivo o sin permisos para esta operación.");

            var ordenes =
                await _context.OrdenesTrabajo

                    .Include(o => o.IngresoVehiculo)
                        .ThenInclude(i => i.Turno)
                            .ThenInclude(t => t.Vehiculo)

                    .Include(o => o.IngresoVehiculo)
                        .ThenInclude(i => i.Turno)
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
                int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAutorizadoAsync(
                usuarioSolicitanteId, "ORDEN_VER");

            if (usuario == null)
                return ServiceResult<List<OrdenTrabajo>>.Error(
                    "Usuario inactivo o sin permisos para esta operación.");

            var personaSolicitanteId = usuario.PersonaId;

            bool esAdmin =
                await _permisoService.EsAdministradorAsync(usuarioSolicitanteId);

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

                    .Include(o => o.IngresoVehiculo)
                        .ThenInclude(i => i.Turno)
                            .ThenInclude(t => t.Vehiculo)

                    .Include(o => o.IngresoVehiculo)
                        .ThenInclude(i => i.Turno)
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
                int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAutorizadoAsync(
                usuarioSolicitanteId, "ORDEN_ASIGNAR_MECANICO");

            if (usuario == null)
                return ServiceResult<List<Persona>>.Error(
                    "Usuario inactivo o sin permisos para esta operación.");

            var mecanicos =
                await _context.PersonaRoles

                    .Include(pr => pr.Persona)
                    .Include(pr => pr.Rol)

                    .Where(pr =>
                        pr.Persona.Activo &&
                        pr.Rol.Nombre == RolesSistema.MECANICO &&
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
        // ORDEN_ASIGNAR_MECANICO; destinatario elegible como mec?nico.
        // =====================================================

        public async Task<ServiceResult>
            AsignarMecanicoAsync(
                int ordenTrabajoId,
                int mecanicoId,
                int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAutorizadoAsync(
                usuarioSolicitanteId, "ORDEN_ASIGNAR_MECANICO");

            if (usuario == null)
                return ServiceResult.Error(
                    "Usuario inactivo o sin permisos para esta operación.");

            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult.Error(
                    "Orden de trabajo no encontrada.");
            }

            if (orden.FechaFin.HasValue ||
                orden.EstadoActual == EstadoOrden.Finalizado ||
                orden.EstadoActual == EstadoOrden.Entregado)
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
                int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAutorizadoAsync(
                usuarioSolicitanteId, "ORDEN_MODIFICAR");

            if (usuario == null)
                return ServiceResult.Error(
                    "Usuario inactivo o sin permisos para esta operación.");

            var mecanicoId = usuario.PersonaId;

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
                int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAutorizadoAsync(
                usuarioSolicitanteId, "ORDEN_CAMBIAR_ESTADO");

            if (usuario == null)
                return ServiceResult.Error(
                    "Usuario inactivo o sin permisos para esta operación.");

            var mecanicoId = usuario.PersonaId;

            var esAdmin = await _permisoService.EsAdministradorAsync(usuarioSolicitanteId);

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

            if (!orden.MecanicoId.HasValue)
                return ServiceResult.Error("La orden debe tener un mec?nico asignado.");

            if (!esAdmin &&
                (!await EsMecanicoActivoAsync(mecanicoId) ||
                 orden.MecanicoId != mecanicoId))
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
                // El modelo actual no registra un actor administrativo.
                historial.MecanicoId =
                    esAdmin ? null : mecanicoId;
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
                int usuarioSolicitanteId,
                int? horasReales = null)
        {
            var usuario = await ObtenerUsuarioAutorizadoAsync(
                usuarioSolicitanteId, "ORDEN_FINALIZAR");

            if (usuario == null)
                return ServiceResult.Error(
                    "Usuario inactivo o sin permisos para esta operación.");

            var mecanicoId = usuario.PersonaId;

            var esAdmin = await _permisoService.EsAdministradorAsync(usuarioSolicitanteId);

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

            if (!orden.MecanicoId.HasValue)
                return ServiceResult.Error("La orden debe tener un mec?nico asignado.");

            if (!esAdmin &&
                (!await EsMecanicoActivoAsync(mecanicoId) ||
                 orden.MecanicoId != mecanicoId))
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
                // El modelo actual no registra un actor administrativo.
                historial.MecanicoId =
                    esAdmin ? null : mecanicoId;
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
                int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAutorizadoAsync(
                usuarioSolicitanteId, "ORDEN_ENTREGAR");

            if (usuario == null)
                return ServiceResult.Error(
                    "Usuario inactivo o sin permisos para esta operación.");

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

                        .Include(o => o.IngresoVehiculo)

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
                    orden.IngresoVehiculo;

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
                int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAutorizadoAsync(
                usuarioSolicitanteId, "ORDEN_MODIFICAR");

            if (usuario == null)
                return ServiceResult.Error(
                    "Usuario inactivo o sin permisos para esta operación.");

            var personaSolicitanteId = usuario.PersonaId;

            if (!Enum.IsDefined(typeof(NivelUrgencia), urgencia))
                return ServiceResult.Error("El nivel de urgencia no es válido.");

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
                await _permisoService.EsAdministradorAsync(usuarioSolicitanteId);

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
                int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAutorizadoAsync(
                usuarioSolicitanteId, "ORDEN_MODIFICAR");

            if (usuario == null)
                return ServiceResult.Error(
                    "Usuario inactivo o sin permisos para esta operación.");

            var personaSolicitanteId = usuario.PersonaId;

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
                await _permisoService.EsAdministradorAsync(usuarioSolicitanteId);

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

        private async Task<Usuario?> ObtenerUsuarioAutorizadoAsync(
            int usuarioSolicitanteId,
            string patente)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Persona)
                .FirstOrDefaultAsync(u =>
                    u.Id == usuarioSolicitanteId &&
                    u.Activo && u.Persona.Activo);

            if (usuario == null ||
                !await _permisoService.TienePermisoAsync(usuarioSolicitanteId, patente))
                return null;

            return usuario;
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
                        RolesSistema.MECANICO &&

                    pr.Rol.Activo &&

                    pr.FechaBaja == null);
        }


        private async Task<OrdenTrabajo?>
            ObtenerOrdenCompletaAsync(
                int id)
        {
            return await _context.OrdenesTrabajo

                .Include(o => o.IngresoVehiculo)
                    .ThenInclude(i => i.Turno)
                        .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Marca)

                .Include(o => o.IngresoVehiculo)
                    .ThenInclude(i => i.Turno)
                        .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Modelo)

                .Include(o => o.IngresoVehiculo)
                    .ThenInclude(i => i.Turno)
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

                .Include(o => o.Evidencias)

                .FirstOrDefaultAsync(o =>
                    o.Id == id);
        }
    }
}