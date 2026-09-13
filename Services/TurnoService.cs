using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class TurnoService
    {
        private readonly MecaniCarContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly AgendaService _agendaService;
        private readonly PermisoService _permisoService;
        private readonly DominioVehicularService _dominioVehicularService;

        public TurnoService(
            MecaniCarContext context,
            AgendaService agendaService,
            PermisoService permisoService,
            DominioVehicularService dominioVehicularService, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
            _agendaService = agendaService;
            _permisoService = permisoService;
            _dominioVehicularService = dominioVehicularService;
        }


        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<List<Turno>>> ObtenerTodosAsync(
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "TURNO_VER"))
            {
                return ServiceResult<List<Turno>>.Error(
                    "No posee permisos para consultar turnos.");
            }

            var turnos = await _context.Turnos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Marca)
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Modelo)
                .Include(t => t.Cliente)
                .Include(t => t.IngresoVehiculo)
                    .ThenInclude(i => i.OrdenTrabajo)
                .OrderBy(t => t.FechaInicio)
                .ToListAsync();

            return ServiceResult<List<Turno>>.Ok(turnos);
        }


        public async Task<ServiceResult<Turno>> ObtenerPorIdAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "TURNO_VER"))
            {
                return ServiceResult<Turno>.Error(
                    "No posee permisos para consultar turnos.");
            }

            return await ObtenerPorIdInternoAsync(id);
        }

        private async Task<ServiceResult<Turno>> ObtenerPorIdInternoAsync(
            int id)
        {
            var turno = await _context.Turnos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Marca)
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Modelo)
                .Include(t => t.Cliente)
                .Include(t => t.IngresoVehiculo)
                    .ThenInclude(i => i.OrdenTrabajo)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (turno == null)
            {
                return ServiceResult<Turno>.Error(
                    "Turno no encontrado.");
            }

            return ServiceResult<Turno>.Ok(turno);
        }


        public async Task<ServiceResult<List<Turno>>> ObtenerTurnosPropiosAsync(
            int usuarioId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioId,
                "CLIENTE_TURNO_VER"))
            {
                return ServiceResult<List<Turno>>.Error(
                    "No posee permisos para consultar sus turnos.");
            }

            var personaId = await ObtenerPersonaIdAsync(usuarioId);

            if (!personaId.HasValue)
            {
                return ServiceResult<List<Turno>>.Error(
                    "Usuario no encontrado o inactivo.");
            }

            return ServiceResult<List<Turno>>.Ok(
                await ObtenerTurnosDePersonaInternoAsync(personaId.Value));
        }

        public async Task<ServiceResult<Turno>> ObtenerTurnoPropioAsync(
            int usuarioId,
            int turnoId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioId,
                "CLIENTE_TURNO_VER"))
            {
                return ServiceResult<Turno>.Error(
                    "No posee permisos para consultar sus turnos.");
            }

            var personaId = await ObtenerPersonaIdAsync(usuarioId);

            if (!personaId.HasValue)
            {
                return ServiceResult<Turno>.Error(
                    "Usuario no encontrado o inactivo.");
            }

            var turno = await ObtenerTurnoPropioInternoAsync(
                personaId.Value,
                turnoId);

            return turno == null
                ? ServiceResult<Turno>.Error("Turno no encontrado.")
                : ServiceResult<Turno>.Ok(turno);
        }

        private async Task<List<Turno>> ObtenerTurnosDePersonaInternoAsync(
            int personaId)
        {
            return await _context.Turnos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Marca)
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Modelo)
                .Where(t => t.ClienteId == personaId)
                .OrderByDescending(t => t.FechaInicio)
                .ToListAsync();
        }

        private async Task<Turno?> ObtenerTurnoPropioInternoAsync(
            int personaId,
            int turnoId)
        {
            return await _context.Turnos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Marca)
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Modelo)
                .Include(t => t.Cliente)
                .Include(t => t.IngresoVehiculo)
                    .ThenInclude(i => i.OrdenTrabajo)
                .FirstOrDefaultAsync(t =>
                    t.Id == turnoId &&
                    t.ClienteId == personaId);
        }


        public async Task<ServiceResult<List<Turno>>> ObtenerTurnosDeFechaAsync(
            DateTime fecha,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "TURNO_VER"))
            {
                return ServiceResult<List<Turno>>.Error(
                    "No posee permisos para consultar turnos.");
            }

            var desde = fecha.Date;
            var hasta = desde.AddDays(1);

            var turnos = await _context.Turnos
                .Include(t => t.Vehiculo)
                .Include(t => t.Cliente)
                .Where(t =>
                    t.FechaInicio >= desde &&
                    t.FechaInicio < hasta)
                .OrderBy(t => t.FechaInicio)
                .ToListAsync();

            return ServiceResult<List<Turno>>.Ok(turnos);
        }


        // =====================================
        // CREAR TURNO
        // =====================================

        public async Task<ServiceResult> CrearAsync(
            int clienteId,
            int vehiculoId,
            TipoTurno tipo,
            DateTime fechaInicio,
            string motivo,
            int usuarioId,
            string? observaciones = null)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioId,
                "TURNO_CREAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para crear turnos.");
            }

            return await CrearInternoAsync(
                clienteId,
                vehiculoId,
                tipo,
                fechaInicio,
                motivo,
                usuarioId,
                observaciones);
        }

        public async Task<ServiceResult> CrearPropioAsync(
            int usuarioId,
            int vehiculoId,
            TipoTurno tipo,
            DateTime fechaInicio,
            string motivo,
            string? observaciones = null)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioId,
                "CLIENTE_TURNO_CREAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para crear sus turnos.");
            }

            var personaId = await ObtenerPersonaIdAsync(usuarioId);

            if (!personaId.HasValue)
            {
                return ServiceResult.Error(
                    "Usuario no encontrado o inactivo.");
            }

            if (!await _dominioVehicularService.EsTitularActualAsync(
                personaId.Value,
                vehiculoId))
            {
                return ServiceResult.Error(
                    "El cliente no es el titular actual del vehículo.");
            }

            return await CrearInternoAsync(
                personaId.Value,
                vehiculoId,
                tipo,
                fechaInicio,
                motivo,
                usuarioId,
                observaciones);
        }

        private async Task<ServiceResult> CrearInternoAsync(
            int clienteId,
            int vehiculoId,
            TipoTurno tipo,
            DateTime fechaInicio,
            string motivo,
            int usuarioId,
            string? observaciones)
        {
            // ---------------------------------
            // VALIDACIONES BÁSICAS
            // ---------------------------------

            if (string.IsNullOrWhiteSpace(motivo))
            {
                return ServiceResult.Error(
                    "Debe indicar el motivo del turno.");
            }

            if (fechaInicio <= DateTime.Now)
            {
                return ServiceResult.Error(
                    "La fecha del turno debe ser futura.");
            }

            var cliente = await _context.Personas
                .FirstOrDefaultAsync(p =>
                    p.Id == clienteId &&
                    p.Activo);

            if (cliente == null)
            {
                return ServiceResult.Error(
                    "El cliente no existe o está inactivo.");
            }

            var vehiculo = await _context.Vehiculos
                .FirstOrDefaultAsync(v =>
                    v.Id == vehiculoId &&
                    v.Activo);

            if (vehiculo == null)
            {
                return ServiceResult.Error(
                    "El vehículo no existe o está inactivo.");
            }


            // ---------------------------------
            // VERIFICAR TITULARIDAD
            // ---------------------------------

            var esTitular = await _dominioVehicularService
                .EsTitularActualAsync(clienteId, vehiculoId);

            if (!esTitular)
            {
                return ServiceResult.Error(
                    "El cliente no es el titular actual del vehículo.");
            }


            // ---------------------------------
            // EVITAR TURNO DUPLICADO
            // ---------------------------------

            var tieneTurnoActivo = await _context.Turnos
                .AnyAsync(t =>
                    t.VehiculoId == vehiculoId &&
                    t.Estado != EstadoTurno.Cancelado &&
                    t.Estado != EstadoTurno.ClienteAusente &&
                    t.Estado != EstadoTurno.Finalizado &&
                    t.FechaInicio >= DateTime.Now);

            if (tieneTurnoActivo)
            {
                return ServiceResult.Error(
                    "El vehículo ya posee un turno activo.");
            }


            // ---------------------------------
            // DISPONIBILIDAD
            // ---------------------------------

            var disponibilidad =
                await _agendaService.ValidarDisponibilidadAsync(
                    fechaInicio);

            if (!disponibilidad.Exitoso)
            {
                return disponibilidad;
            }


            // ---------------------------------
            // CREAR TURNO
            // ---------------------------------

            var turno = new Turno
            {
                ClienteId = clienteId,
                VehiculoId = vehiculoId,
                Tipo = tipo,
                Motivo = motivo.Trim(),
                FechaInicio = fechaInicio,
                Estado = EstadoTurno.Pendiente,
                FechaCreacion = DateTime.Now,
                CreadoPorUsuarioId = usuarioId,
                Observaciones =
                    string.IsNullOrWhiteSpace(observaciones)
                        ? null
                        : observaciones.Trim()
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.Turnos.Add(turno);

            await _context.SaveChangesAsync();


            // ---------------------------------
            // HISTORIAL
            // ---------------------------------

            _context.TurnoEstados.Add(
                new TurnoEstadoHistorial
                {
                    TurnoId = turno.Id,
                    Estado = EstadoTurno.Pendiente,
                    FechaCambio = DateTime.Now,
                    UsuarioId = usuarioId,
                    Observaciones = "Turno creado."
                });

            _auditoria.RegistrarOperacion("TURNO_CREADO", "Turno", turno.Id, usuarioId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult.Ok(
                "Turno creado correctamente.");
        }


        // =====================================
        // CONFIRMAR
        // =====================================

        public async Task<ServiceResult> ConfirmarAsync(
            int turnoId,
            int usuarioId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioId,
                "TURNO_CONFIRMAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para confirmar turnos.");
            }

            var turno = await _context.Turnos
                .FirstOrDefaultAsync(t => t.Id == turnoId);

            if (turno == null)
            {
                return ServiceResult.Error(
                    "Turno no encontrado.");
            }

            if (turno.Estado != EstadoTurno.Pendiente)
            {
                return ServiceResult.Error(
                    "Solo se pueden confirmar turnos pendientes.");
            }

            turno.Estado = EstadoTurno.Confirmado;

            _context.TurnoEstados.Add(
                new TurnoEstadoHistorial
                {
                    TurnoId = turno.Id,
                    Estado = EstadoTurno.Confirmado,
                    FechaCambio = DateTime.Now,
                    UsuarioId = usuarioId,
                    Observaciones = "Turno confirmado."
                });

            _auditoria.RegistrarOperacion("TURNO_CONFIRMADO", "Turno", turno.Id, usuarioId);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Turno confirmado correctamente.");
        }


        // =====================================
        // CANCELAR
        // =====================================

        public async Task<ServiceResult> CancelarAsync(
            int turnoId,
            int usuarioId,
            string? motivo = null)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioId,
                "TURNO_CANCELAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para cancelar turnos.");
            }

            return await CancelarInternoAsync(turnoId, usuarioId, motivo);
        }

        public async Task<ServiceResult> CancelarPropioAsync(
            int turnoId,
            int usuarioId,
            string? motivo = null)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioId,
                "CLIENTE_TURNO_CANCELAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para cancelar sus turnos.");
            }

            var personaId = await ObtenerPersonaIdAsync(usuarioId);

            if (!personaId.HasValue)
            {
                return ServiceResult.Error(
                    "Usuario no encontrado o inactivo.");
            }

            var turno = await ObtenerTurnoPropioInternoAsync(
                personaId.Value,
                turnoId);

            if (turno == null)
            {
                return ServiceResult.Error("Turno no encontrado.");
            }

            return await CancelarInternoAsync(turnoId, usuarioId, motivo);
        }

        private async Task<ServiceResult> CancelarInternoAsync(
            int turnoId,
            int usuarioId,
            string? motivo)
        {
            var turno = await _context.Turnos
                .Include(t => t.IngresoVehiculo)
                .FirstOrDefaultAsync(t => t.Id == turnoId);

            if (turno == null)
            {
                return ServiceResult.Error(
                    "Turno no encontrado.");
            }

            if (turno.Estado == EstadoTurno.Cancelado)
            {
                return ServiceResult.Error(
                    "El turno ya está cancelado.");
            }

            if (turno.Estado == EstadoTurno.Finalizado)
            {
                return ServiceResult.Error(
                    "No se puede cancelar un turno finalizado.");
            }

            if (turno.Estado == EstadoTurno.ClienteAusente)
            {
                return ServiceResult.Error(
                    "No se puede cancelar un turno marcado como cliente ausente.");
            }

            if (turno.IngresoVehiculo != null)
            {
                return ServiceResult.Error(
                    "No se puede cancelar un turno cuyo vehículo ya ingresó al taller.");
            }

            turno.Estado = EstadoTurno.Cancelado;

            _context.TurnoEstados.Add(
                new TurnoEstadoHistorial
                {
                    TurnoId = turno.Id,
                    Estado = EstadoTurno.Cancelado,
                    FechaCambio = DateTime.Now,
                    UsuarioId = usuarioId,
                    Observaciones =
                        string.IsNullOrWhiteSpace(motivo)
                            ? "Turno cancelado."
                            : motivo.Trim()
                });

            _auditoria.RegistrarOperacion("TURNO_CANCELADO", "Turno", turno.Id, usuarioId);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Turno cancelado correctamente.");
        }


        // =====================================
        // CLIENTE AUSENTE
        // =====================================

        public async Task<ServiceResult> MarcarClienteAusenteAsync(
            int turnoId,
            int usuarioId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioId,
                "TURNO_MODIFICAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar turnos.");
            }

            var turno = await _context.Turnos
                .FirstOrDefaultAsync(t => t.Id == turnoId);

            if (turno == null)
            {
                return ServiceResult.Error(
                    "Turno no encontrado.");
            }

            if (turno.Estado != EstadoTurno.Confirmado)
            {
                return ServiceResult.Error(
                    "El turno debe estar confirmado.");
            }

            if (turno.FechaInicio > DateTime.Now)
            {
                return ServiceResult.Error(
                    "No se puede marcar como ausente antes del horario del turno.");
            }

            turno.Estado = EstadoTurno.ClienteAusente;

            _context.TurnoEstados.Add(
                new TurnoEstadoHistorial
                {
                    TurnoId = turno.Id,
                    Estado = EstadoTurno.ClienteAusente,
                    FechaCambio = DateTime.Now,
                    UsuarioId = usuarioId,
                    Observaciones = "Cliente ausente."
                });

            _auditoria.RegistrarOperacion("CLIENTE_AUSENTE", "Turno", turno.Id, usuarioId);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Cliente marcado como ausente.");
        }


        // =====================================
        // REPROGRAMAR
        // =====================================

        public async Task<ServiceResult> ReprogramarAsync(
            int turnoId,
            DateTime nuevaFechaInicio,
            int usuarioId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioId,
                "TURNO_MODIFICAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar turnos.");
            }

            var turno = await _context.Turnos
                .Include(t => t.IngresoVehiculo)
                .FirstOrDefaultAsync(t => t.Id == turnoId);

            if (turno == null)
            {
                return ServiceResult.Error(
                    "Turno no encontrado.");
            }

            if (turno.Estado != EstadoTurno.Pendiente &&
                turno.Estado != EstadoTurno.Confirmado)
            {
                return ServiceResult.Error(
                    "Solo se pueden reprogramar turnos pendientes o confirmados.");
            }

            if (turno.IngresoVehiculo != null)
            {
                return ServiceResult.Error(
                    "No se puede reprogramar un turno cuyo vehículo ya ingresó al taller.");
            }

            if (nuevaFechaInicio <= DateTime.Now)
            {
                return ServiceResult.Error(
                    "La nueva fecha del turno debe ser futura.");
            }


            // ---------------------------------
            // VALIDAR NUEVO HORARIO
            // ---------------------------------

            var disponibilidad =
                await _agendaService.ValidarDisponibilidadAsync(
                    nuevaFechaInicio,
                    turno.Id);

            if (!disponibilidad.Exitoso)
            {
                return disponibilidad;
            }


            // ---------------------------------
            // GUARDAR CAMBIO
            // ---------------------------------

            var fechaAnterior = turno.FechaInicio;

            turno.FechaInicio = nuevaFechaInicio;

            _context.TurnoEstados.Add(
                new TurnoEstadoHistorial
                {
                    TurnoId = turno.Id,
                    Estado = turno.Estado,
                    FechaCambio = DateTime.Now,
                    UsuarioId = usuarioId,
                    Observaciones =
                        $"Turno reprogramado de " +
                        $"{fechaAnterior:dd/MM/yyyy HH:mm} " +
                        $"a {nuevaFechaInicio:dd/MM/yyyy HH:mm}."
                });

            _auditoria.RegistrarOperacion("TURNO_REPROGRAMADO", "Turno", turno.Id, usuarioId);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Turno reprogramado correctamente.");
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
    }
}
