using System.ComponentModel.DataAnnotations;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Models.ViewModels;
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
                .Include(i => i.Vehiculo)
                    .ThenInclude(v => v.Marca)
                .Include(i => i.Vehiculo)
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
                .Include(i => i.Vehiculo)
                    .ThenInclude(v => v.Marca)
                .Include(i => i.Vehiculo)
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


        public async Task<ServiceResult<RegistrarIngresoViewModel>> PrepararRegistroAsync(
            int turnoId,
            int usuarioSolicitanteId)
        {
            if (!await TienePermisoAsync(usuarioSolicitanteId, "INGRESO_REGISTRAR"))
                return ServiceResult<RegistrarIngresoViewModel>.Error("No tiene permisos para registrar ingresos.");

            var turno = await _context.Turnos.AsNoTracking()
                .Include(t => t.Cliente)
                .Include(t => t.Vehiculo)
                .Include(t => t.IngresoVehiculo)
                .FirstOrDefaultAsync(t => t.Id == turnoId);
            if (turno == null)
                return ServiceResult<RegistrarIngresoViewModel>.Error("Turno no encontrado.");

            var vehiculos = await _context.DominiosVehiculares.AsNoTracking()
                .Where(d => d.PersonaId == turno.ClienteId && d.FechaHasta == null && d.Vehiculo.Activo)
                .Select(d => d.Vehiculo)
                .Include(v => v.Marca)
                .Include(v => v.Modelo)
                .OrderBy(v => v.Patente)
                .ToListAsync();

            return ServiceResult<RegistrarIngresoViewModel>.Ok(new RegistrarIngresoViewModel
            {
                TurnoId = turno.Id,
                Turno = turno,
                Vehiculos = vehiculos,
                VehiculoId = turno.VehiculoId.HasValue && vehiculos.Any(v => v.Id == turno.VehiculoId.Value)
                    ? turno.VehiculoId
                    : null,
                Kilometraje = turno.Vehiculo?.Kilometraje
            });
        }


        // =====================================
        // REGISTRAR INGRESO + CREAR ORDEN
        // =====================================

        public async Task<ServiceResult<OrdenTrabajo>>
            RegistrarIngresoYCrearOrdenAsync(
                RegistrarIngresoViewModel model,
                int usuarioSolicitanteId)
        {
            if (!await TienePermisoAsync(
                    usuarioSolicitanteId,
                    "INGRESO_REGISTRAR"))
            {
                return ServiceResult<OrdenTrabajo>.Error(
                    "No tiene permisos para registrar ingresos.");
            }

            var validaciones = new List<ValidationResult>();
            if (!Validator.TryValidateObject(model, new ValidationContext(model), validaciones, validateAllProperties: true))
                return ServiceResult<OrdenTrabajo>.Error(validaciones.FirstOrDefault()?.ErrorMessage ?? "Revise los datos de recepción.");

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                // =====================================
                // BUSCAR TURNO
                // =====================================

                var turno = await _context.Turnos
                    .FromSqlInterpolated($"SELECT * FROM [Turnos] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {model.TurnoId}")
                    .Include(t => t.Vehiculo)
                    .Include(t => t.IngresoVehiculo)
                    .Include(t => t.Cliente)
                    .FirstOrDefaultAsync(t =>
                        t.Id == model.TurnoId);

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

                var vehiculo = await _context.Vehiculos
                    .FromSqlInterpolated($"SELECT * FROM [Vehiculos] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {model.VehiculoId!.Value}")
                    .Include(v => v.Marca)
                    .Include(v => v.Modelo)
                    .FirstOrDefaultAsync(v => v.Activo);
                if (vehiculo == null)
                    return ServiceResult<OrdenTrabajo>.Error("El vehículo seleccionado no existe o está inactivo.");

                var titularValido = await _context.DominiosVehiculares.AnyAsync(d =>
                    d.PersonaId == turno.ClienteId && d.VehiculoId == vehiculo.Id && d.FechaHasta == null);
                if (!titularValido)
                    return ServiceResult<OrdenTrabajo>.Error("El vehículo recibido no pertenece al cliente del turno.");

                if (await _context.IngresosVehiculo.AnyAsync(i => i.VehiculoId == vehiculo.Id && i.FechaEgreso == null))
                    return ServiceResult<OrdenTrabajo>.Error("El vehículo ya tiene un ingreso activo en el taller.");

                if (vehiculo.Kilometraje.HasValue && model.Kilometraje!.Value < vehiculo.Kilometraje.Value && !model.ConfirmarKilometrajeMenor)
                    return ServiceResult<OrdenTrabajo>.Error("Confirme expresamente el kilometraje menor al registrado anteriormente.");

                if (model.EstadoExterior == EstadoExteriorRecepcion.ConObservaciones && string.IsNullOrWhiteSpace(model.DescripcionEstadoExterior))
                    return ServiceResult<OrdenTrabajo>.Error("Describa el estado exterior del vehículo.");
                if (model.EstadoExterior == EstadoExteriorRecepcion.SinDanosVisiblesDeclarados && !string.IsNullOrWhiteSpace(model.DescripcionEstadoExterior))
                    return ServiceResult<OrdenTrabajo>.Error("Quite la descripción exterior o seleccione el estado con observaciones.");
                if (!model.VerificadoConCliente)
                    return ServiceResult<OrdenTrabajo>.Error("Debe verificar los datos de recepción con el cliente.");

                var accesorios = model.AccesoriosSeleccionados.Aggregate(AccesoriosRecepcion.Ninguno, (actual, item) => actual | item);
                var otrosSeleccionado = accesorios.HasFlag(AccesoriosRecepcion.Otros);
                if (otrosSeleccionado != !string.IsNullOrWhiteSpace(model.OtrosAccesorios))
                    return ServiceResult<OrdenTrabajo>.Error("La descripción de Otros debe corresponder con la selección de accesorios.");


                // =====================================
                // CREAR INGRESO
                // =====================================

                var ingreso = new IngresoVehiculo
                {
                    TurnoId = turno.Id,
                    VehiculoId = vehiculo.Id,
                    RegistradoPorUsuarioId = usuarioSolicitanteId,
                    ClienteNombreSnapshot = $"{turno.Cliente.Nombre} {turno.Cliente.Apellido}".Trim(),
                    ClienteDniSnapshot = turno.Cliente.Dni.Trim(),
                    VehiculoPatenteSnapshot = vehiculo.Patente.Trim(),
                    VehiculoDescripcionSnapshot = $"{vehiculo.Marca.Nombre} {vehiculo.Modelo.Nombre} {vehiculo.Anio}".Trim(),
                    Kilometraje = model.Kilometraje!.Value,
                    NivelCombustible = model.NivelCombustible!.Value,
                    EstadoExterior = model.EstadoExterior!.Value,
                    ObservacionesEstadoExterior = string.IsNullOrWhiteSpace(model.DescripcionEstadoExterior) ? null : model.DescripcionEstadoExterior.Trim(),
                    Accesorios = accesorios,
                    OtrosAccesorios = otrosSeleccionado ? model.OtrosAccesorios!.Trim() : null,
                    DatosVerificadosConCliente = true,

                    FechaIngreso =
                        DateTime.Now,

                    ClienteEspera =
                        turno.Tipo == TipoTurno.Servicio && model.ClienteEspera,

                    ObservacionesRecepcion =
                        string.IsNullOrWhiteSpace(model.ObservacionesRecepcion)
                            ? null
                            : model.ObservacionesRecepcion.Trim()
                };

                _context.IngresosVehiculo.Add(
                    ingreso);


                // =====================================
                // FINALIZAR TURNO
                // =====================================

                turno.Estado =
                    EstadoTurno.Finalizado;
                vehiculo.Kilometraje = model.Kilometraje.Value;

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
                    "No se pudo completar la recepción porque el turno, la orden o el vehículo ya tiene una asociación activa.");
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
                .Include(i => i.Vehiculo)
                    .ThenInclude(v => v.Marca)
                .Include(i => i.Vehiculo)
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
                    i.VehiculoId == vehiculoId &&
                    !i.FechaEgreso.HasValue);
        }
    }
}