using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class IngresoVehiculoService
    {
        private readonly MecaniCarContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly PermisoService _permisoService;

        public IngresoVehiculoService(
            MecaniCarContext context,
            PermisoService permisoService, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
            _permisoService = permisoService;
        }

        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<IngresoVehiculo>> ObtenerPorIdAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await TienePermisoAsync(
                    usuarioSolicitanteId,
                    "INGRESO_VER"))
            {
                return ServiceResult<IngresoVehiculo>.Error(
                    "No tiene permisos para consultar ingresos.");
            }

            var ingreso = await _context.IngresosVehiculo
                .Include(i => i.OrdenTrabajo)
                .Include(i => i.Turno)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Marca)

                .Include(i => i.Turno)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Modelo)

                .Include(i => i.Turno)
                    .ThenInclude(t => t.Cliente)


                .FirstOrDefaultAsync(i => i.Id == id);

            if (ingreso == null)
            {
                return ServiceResult<IngresoVehiculo>.Error(
                    "Ingreso de vehículo no encontrado.");
            }

            return ServiceResult<IngresoVehiculo>.Ok(
                ingreso);
        }


        public async Task<ServiceResult<IngresoVehiculo>> ObtenerPorTurnoAsync(
            int turnoId,
            int usuarioSolicitanteId)
        {
            if (!await TienePermisoAsync(
                    usuarioSolicitanteId,
                    "INGRESO_VER"))
            {
                return ServiceResult<IngresoVehiculo>.Error(
                    "No tiene permisos para consultar ingresos.");
            }

            var ingreso = await _context.IngresosVehiculo
                .Include(i => i.OrdenTrabajo)
                .Include(i => i.Turno)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Marca)

                .Include(i => i.Turno)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Modelo)

                .Include(i => i.Turno)
                    .ThenInclude(t => t.Cliente)

                .FirstOrDefaultAsync(i =>
                    i.TurnoId == turnoId);

            if (ingreso == null)
            {
                return ServiceResult<IngresoVehiculo>.Error(
                    "El vehículo todavía no posee un ingreso registrado.");
            }

            return ServiceResult<IngresoVehiculo>.Ok(
                ingreso);
        }


        // =====================================
        // REGISTRAR INGRESO + CREAR ORDEN
        // =====================================

        public async Task<ServiceResult<OrdenTrabajo>>
            RegistrarIngresoYCrearOrdenAsync(
                int turnoId,
                bool clienteEspera,
                string? observaciones,
                int usuarioSolicitanteId)
        {
            if (!await TienePermisoAsync(
                    usuarioSolicitanteId,
                    "INGRESO_REGISTRAR"))
            {
                return ServiceResult<OrdenTrabajo>.Error(
                    "No tiene permisos para registrar ingresos.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // =====================================
                // BUSCAR TURNO
                // =====================================

                var turno = await _context.Turnos
                    .Include(t => t.IngresoVehiculo)
                    .FirstOrDefaultAsync(t =>
                        t.Id == turnoId);

                if (turno == null)
                {
                    return ServiceResult<OrdenTrabajo>.Error(
                        "Turno no encontrado.");
                }


                // =====================================
                // VALIDAR ESTADO
                // =====================================

                if (turno.Estado == EstadoTurno.Cancelado)
                {
                    return ServiceResult<OrdenTrabajo>.Error(
                        "No se puede registrar el ingreso de un turno cancelado.");
                }

                if (turno.Estado == EstadoTurno.ClienteAusente)
                {
                    return ServiceResult<OrdenTrabajo>.Error(
                        "El cliente fue marcado como ausente.");
                }

                if (turno.Estado == EstadoTurno.Finalizado)
                {
                    return ServiceResult<OrdenTrabajo>.Error(
                        "El turno ya fue atendido.");
                }

                if (turno.Estado != EstadoTurno.Pendiente &&
                    turno.Estado != EstadoTurno.Confirmado)
                {
                    return ServiceResult<OrdenTrabajo>.Error(
                        "Solo se puede registrar el ingreso de un turno pendiente o confirmado.");
                }


                // =====================================
                // EVITAR DOBLE INGRESO
                // =====================================

                if (turno.IngresoVehiculo != null)
                {
                    return ServiceResult<OrdenTrabajo>.Error(
                        "El vehículo ya posee un ingreso registrado.");
                }


                // =====================================
                // CREAR INGRESO
                // =====================================

                var ingreso = new IngresoVehiculo
                {
                    TurnoId =
                        turnoId,

                    FechaIngreso =
                        DateTime.Now,

                    ClienteEspera =
                        clienteEspera,

                    ObservacionesRecepcion =
                        string.IsNullOrWhiteSpace(observaciones)
                            ? null
                            : observaciones.Trim()
                };

                _context.IngresosVehiculo.Add(
                    ingreso);


                // =====================================
                // FINALIZAR TURNO
                // =====================================

                turno.Estado =
                    EstadoTurno.Finalizado;

                _context.TurnoEstados.Add(
                    new TurnoEstadoHistorial
                    {
                        TurnoId =
                            turno.Id,

                        Estado =
                            EstadoTurno.Finalizado,

                        FechaCambio =
                            DateTime.Now,

                        UsuarioId =
                            usuarioSolicitanteId,

                        Observaciones =
                            "Vehículo ingresado al taller."
                    });


                // =====================================
                // GUARDAR INGRESO
                // =====================================

                await _context.SaveChangesAsync();


                // =====================================
                // CREAR ORDEN DE TRABAJO
                // =====================================

                var orden = new OrdenTrabajo
                {
                    IngresoVehiculoId =
                        ingreso.Id,

                    EstadoActual =
                        EstadoOrden.Pendiente,

                    FechaInicio =
                        DateTime.Now,

                    CreadaPorUsuarioId =
                        usuarioSolicitanteId,

                    Urgencia =
                        NivelUrgencia.Media
                };

                _context.OrdenesTrabajo.Add(
                    orden);


                await _context.SaveChangesAsync();


                // =====================================
                // HISTORIAL DE LA ORDEN
                // =====================================

                _context.OrdenTrabajoEstados.Add(
                    new OrdenTrabajoEstadoHistorial
                    {
                        OrdenTrabajoId =
                            orden.Id,

                        Estado =
                            EstadoOrden.Pendiente,

                        Fecha =
                            DateTime.Now
                    });


                await _context.SaveChangesAsync();


                // =====================================
                // CONFIRMAR TRANSACCIÓN
                // =====================================

                _auditoria.RegistrarOperacion("INGRESO_VEHICULO_ORDEN_CREADA", "OrdenTrabajo", orden.Id, usuarioSolicitanteId,
                    $"Ingreso #{ingreso.Id}; turno #{turno.Id} finalizado.");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();


                return ServiceResult<OrdenTrabajo>.Ok(
                    orden,
                    "Ingreso del vehículo y orden de trabajo registrados correctamente.");
            }
            catch (DbUpdateException ex)
                when (EsViolacionUnicidad(ex))
            {
                await transaction.RollbackAsync();

                return ServiceResult<OrdenTrabajo>.Error(
                    "El turno ya posee un ingreso u orden de trabajo registrada.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // =====================================
        // VEHÍCULOS EN EL TALLER
        // =====================================

        public async Task<ServiceResult<List<IngresoVehiculo>>>
            ObtenerVehiculosEnTallerAsync(
                int usuarioSolicitanteId)
        {
            if (!await TienePermisoAsync(
                    usuarioSolicitanteId,
                    "INGRESO_VER"))
            {
                return ServiceResult<List<IngresoVehiculo>>.Error(
                    "No tiene permisos para consultar ingresos.");
            }

            var ingresos = await _context.IngresosVehiculo
                .Include(i => i.OrdenTrabajo)
                .Include(i => i.Turno)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Marca)

                .Include(i => i.Turno)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Modelo)

                .Include(i => i.Turno)
                    .ThenInclude(t => t.Cliente)

                .Where(i =>
                    !i.FechaEgreso.HasValue)

                .OrderBy(i =>
                    i.FechaIngreso)

                .ToListAsync();

            return ServiceResult<List<IngresoVehiculo>>.Ok(
                ingresos);
        }

        private async Task<bool> TienePermisoAsync(
            int usuarioId,
            string patente)
        {
            return await _permisoService.TienePermisoAsync(
                usuarioId,
                patente);
        }

        private static bool EsViolacionUnicidad(
            DbUpdateException exception)
        {
            return exception.InnerException is SqlException sqlException &&
                (sqlException.Number == 2601 ||
                 sqlException.Number == 2627);
        }


        // =====================================
        // VEHÍCULO EN EL TALLER
        // =====================================

        public async Task<bool> VehiculoEstaEnTallerAsync(
            int vehiculoId, int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "INGRESO_VER")) return false;

            return await _context.IngresosVehiculo
                .AnyAsync(i =>
                    i.Turno.VehiculoId == vehiculoId &&
                    !i.FechaEgreso.HasValue);
        }
    }
}