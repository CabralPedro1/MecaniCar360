using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class IngresoVehiculoService
    {
        private readonly MecaniCarContext _context;

        public IngresoVehiculoService(MecaniCarContext context)
        {
            _context = context;
        }

        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<IngresoVehiculo>> ObtenerPorIdAsync(
            int id)
        {
            var ingreso = await _context.IngresosVehiculo
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
            int turnoId)
        {
            var ingreso = await _context.IngresosVehiculo
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
                int usuarioId)
        {
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

                if (turno.Estado ==
                    EstadoTurno.Cancelado)
                {
                    return ServiceResult<OrdenTrabajo>.Error(
                        "No se puede registrar el ingreso de un turno cancelado.");
                }

                if (turno.Estado ==
                    EstadoTurno.Finalizado)
                {
                    return ServiceResult<OrdenTrabajo>.Error(
                        "El turno ya fue atendido.");
                }

                if (turno.Estado ==
                    EstadoTurno.ClienteAusente)
                {
                    return ServiceResult<OrdenTrabajo>.Error(
                        "El cliente fue marcado como ausente.");
                }

                if (turno.Estado !=
                    EstadoTurno.Confirmado)
                {
                    return ServiceResult<OrdenTrabajo>.Error(
                        "Solo se puede registrar el ingreso de un turno confirmado.");
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
                            usuarioId,

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
                    TurnoId =
                        turno.Id,

                    EstadoActual =
                        EstadoOrden.Pendiente,

                    FechaInicio =
                        DateTime.Now,

                    CreadaPorUsuarioId =
                        usuarioId,

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

                await transaction.CommitAsync();


                return ServiceResult<OrdenTrabajo>.Ok(
                    orden,
                    "Ingreso del vehículo y orden de trabajo registrados correctamente.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // =====================================
        // REGISTRAR EGRESO
        // =====================================

        public async Task<ServiceResult> RegistrarEgresoAsync(
            int ingresoId,
            string? observaciones = null)
        {
            var ingreso = await _context.IngresosVehiculo
                .FirstOrDefaultAsync(i =>
                    i.Id == ingresoId);

            if (ingreso == null)
            {
                return ServiceResult.Error(
                    "Ingreso de vehículo no encontrado.");
            }

            if (ingreso.FechaEgreso.HasValue)
            {
                return ServiceResult.Error(
                    "El vehículo ya posee una fecha de egreso.");
            }

            ingreso.FechaEgreso =
                DateTime.Now;

            if (!string.IsNullOrWhiteSpace(observaciones))
            {
                if (string.IsNullOrWhiteSpace(
                    ingreso.ObservacionesRecepcion))
                {
                    ingreso.ObservacionesRecepcion =
                        observaciones.Trim();
                }
                else
                {
                    ingreso.ObservacionesRecepcion +=
                        Environment.NewLine +
                        observaciones.Trim();
                }
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Egreso del vehículo registrado correctamente.");
        }


        // =====================================
        // VEHÍCULOS EN EL TALLER
        // =====================================

        public async Task<ServiceResult<List<IngresoVehiculo>>>
            ObtenerVehiculosEnTallerAsync()
        {
            var ingresos = await _context.IngresosVehiculo
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


        // =====================================
        // VEHÍCULO EN EL TALLER
        // =====================================

        public async Task<bool> VehiculoEstaEnTallerAsync(
            int vehiculoId)
        {
            return await _context.IngresosVehiculo
                .AnyAsync(i =>
                    i.Turno.VehiculoId == vehiculoId &&
                    !i.FechaEgreso.HasValue);
        }
    }
}