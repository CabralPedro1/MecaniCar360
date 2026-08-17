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
        private readonly AgendaService _agendaService;

        public TurnoService(
            MecaniCarContext context,
            AgendaService agendaService)
        {
            _context = context;
            _agendaService = agendaService;
        }

        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<List<Turno>>> ObtenerTodosAsync()
        {
            var turnos = await _context.Turnos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Marca)
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Modelo)
                .Include(t => t.Cliente)
                .OrderBy(t => t.FechaInicio)
                .ToListAsync();

            return ServiceResult<List<Turno>>.Ok(turnos);
        }


        public async Task<ServiceResult<Turno>> ObtenerPorIdAsync(int id)
        {
            var turno = await _context.Turnos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Marca)
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Modelo)
                .Include(t => t.Cliente)
                .Include(t => t.IngresoVehiculo)
                .Include(t => t.OrdenTrabajo)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (turno == null)
                return ServiceResult<Turno>.Error(
                    "Turno no encontrado.");

            return ServiceResult<Turno>.Ok(turno);
        }


        public async Task<ServiceResult<List<Turno>>> ObtenerTurnosDePersonaAsync(
            int personaId)
        {
            var turnos = await _context.Turnos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Marca)
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Modelo)
                .Where(t => t.ClienteId == personaId)
                .OrderByDescending(t => t.FechaInicio)
                .ToListAsync();

            return ServiceResult<List<Turno>>.Ok(turnos);
        }


        public async Task<ServiceResult<List<Turno>>> ObtenerTurnosDeFechaAsync(
            DateTime fecha)
        {
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
        // DURACIÓN
        // =====================================

        public TimeSpan ObtenerDuracion(TipoTurno tipo)
        {
            return tipo switch
            {
                TipoTurno.Diagnostico =>
                    TimeSpan.FromHours(2),

                TipoTurno.Servicio =>
                    TimeSpan.FromHours(2),

                _ =>
                    TimeSpan.FromHours(2)
            };
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
            // ---------------------------------
            // VALIDACIONES BÁSICAS
            // ---------------------------------

            if (string.IsNullOrWhiteSpace(motivo))
                return ServiceResult.Error(
                    "Debe indicar el motivo del turno.");

            var cliente = await _context.Personas
                .FirstOrDefaultAsync(p =>
                    p.Id == clienteId &&
                    p.Activo);

            if (cliente == null)
                return ServiceResult.Error(
                    "El cliente no existe o está inactivo.");

            var vehiculo = await _context.Vehiculos
                .FirstOrDefaultAsync(v =>
                    v.Id == vehiculoId &&
                    v.Activo);

            if (vehiculo == null)
                return ServiceResult.Error(
                    "El vehículo no existe o está inactivo.");

            // ---------------------------------
            // VERIFICAR TITULARIDAD
            // ---------------------------------

            var esTitular = await _context.DominiosVehiculares
                .AnyAsync(d =>
                    d.PersonaId == clienteId &&
                    d.VehiculoId == vehiculoId &&
                    d.FechaHasta == null);

            if (!esTitular)
                return ServiceResult.Error(
                    "El cliente no es el titular actual del vehículo.");

            // ---------------------------------
            // DURACIÓN
            // ---------------------------------

            var duracion = ObtenerDuracion(tipo);

            // ---------------------------------
            // DISPONIBILIDAD
            // ---------------------------------

            var disponibilidad =
                await _agendaService.ValidarDisponibilidadAsync(
                    fechaInicio,
                    duracion);

            if (!disponibilidad.Exitoso)
                return disponibilidad;

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
                DuracionEstimada = duracion,
                Estado = EstadoTurno.Pendiente,
                FechaCreacion = DateTime.Now,
                CreadoPorUsuarioId = usuarioId,
                Observaciones = observaciones
            };

            _context.Turnos.Add(turno);

            await _context.SaveChangesAsync();

            // ---------------------------------
            // HISTORIAL
            // ---------------------------------

            _context.TurnoEstados.Add(new TurnoEstadoHistorial
            {
                TurnoId = turno.Id,
                Estado = EstadoTurno.Pendiente,
                FechaCambio = DateTime.Now,
                UsuarioId = usuarioId,
                Observaciones = "Turno creado."
            });

            await _context.SaveChangesAsync();

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
            var turno = await _context.Turnos
                .FirstOrDefaultAsync(t => t.Id == turnoId);

            if (turno == null)
                return ServiceResult.Error(
                    "Turno no encontrado.");

            if (turno.Estado != EstadoTurno.Pendiente)
                return ServiceResult.Error(
                    "Solo se pueden confirmar turnos pendientes.");

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
            var turno = await _context.Turnos
                .FirstOrDefaultAsync(t => t.Id == turnoId);

            if (turno == null)
                return ServiceResult.Error(
                    "Turno no encontrado.");

            if (turno.Estado == EstadoTurno.Cancelado)
                return ServiceResult.Error(
                    "El turno ya está cancelado.");

            if (turno.Estado == EstadoTurno.Finalizado)
                return ServiceResult.Error(
                    "No se puede cancelar un turno finalizado.");

            turno.Estado = EstadoTurno.Cancelado;

            _context.TurnoEstados.Add(
                new TurnoEstadoHistorial
                {
                    TurnoId = turno.Id,
                    Estado = EstadoTurno.Cancelado,
                    FechaCambio = DateTime.Now,
                    UsuarioId = usuarioId,
                    Observaciones = motivo
                });

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
            var turno = await _context.Turnos
                .FirstOrDefaultAsync(t => t.Id == turnoId);

            if (turno == null)
                return ServiceResult.Error(
                    "Turno no encontrado.");

            if (turno.Estado != EstadoTurno.Confirmado)
                return ServiceResult.Error(
                    "El turno debe estar confirmado.");

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
            var turno = await _context.Turnos
                .FirstOrDefaultAsync(t => t.Id == turnoId);

            if (turno == null)
                return ServiceResult.Error(
                    "Turno no encontrado.");

            if (turno.Estado == EstadoTurno.Cancelado)
                return ServiceResult.Error(
                    "No se puede reprogramar un turno cancelado.");

            if (turno.Estado == EstadoTurno.Finalizado)
                return ServiceResult.Error(
                    "No se puede reprogramar un turno finalizado.");

            // ---------------------------------
            // VALIDAR NUEVO HORARIO
            // ---------------------------------

            var disponibilidad =
                await _agendaService.ValidarDisponibilidadAsync(
                    nuevaFechaInicio,
                    turno.DuracionEstimada);

            if (!disponibilidad.Exitoso)
                return disponibilidad;

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

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Turno reprogramado correctamente.");
        }
    }
}