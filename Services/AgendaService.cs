using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class AgendaService
    {
        private readonly MecaniCarContext _context;
        private readonly PermisoService _permisos;
        private readonly IConfiguration _configuration;

        public AgendaService(
            MecaniCarContext context,
            IConfiguration configuration, PermisoService permisos)
        {
            _context = context;
            _permisos = permisos;
            _configuration = configuration;
        }


        // =====================================
        // CONFIGURACIÓN
        // =====================================

        private int ObtenerTurnosPorFranja()
        {
            return _configuration
                .GetValue<int?>(
                    "ConfiguracionTaller:TurnosPorFranja")
                ?? 2;
        }


        private int ObtenerDuracionFranjaMinutos()
        {
            return _configuration
                .GetValue<int?>(
                    "ConfiguracionTaller:DuracionFranjaTurnoMinutos")
                ?? 30;
        }


        private TimeSpan ObtenerHoraApertura()
        {
            var valor =
                _configuration[
                    "ConfiguracionTaller:HoraApertura"];

            if (TimeSpan.TryParse(
                    valor,
                    out var hora))
            {
                return hora;
            }

            return new TimeSpan(
                8,
                0,
                0);
        }


        private TimeSpan ObtenerHoraCierre()
        {
            var valor =
                _configuration[
                    "ConfiguracionTaller:HoraCierre"];

            if (TimeSpan.TryParse(
                    valor,
                    out var hora))
            {
                return hora;
            }

            return new TimeSpan(
                18,
                0,
                0);
        }


        // =====================================
        // TURNOS DEL PERÍODO
        // =====================================

        public async Task<ServiceResult<List<Turno>>>
            ObtenerTurnosDelPeriodoAsync(
                DateTime desde,
                DateTime hasta, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "TURNO_VER")) return ServiceResult<List<Turno>>.Error("Acceso denegado.");

            if (desde >= hasta)
            {
                return ServiceResult<List<Turno>>.Error(
                    "La fecha de inicio debe ser anterior a la fecha de fin.");
            }

            var turnos = await _context.Turnos
                .Include(t => t.Vehiculo)
                .Include(t => t.Cliente)
                .Where(t =>
                    t.FechaInicio >= desde &&
                    t.FechaInicio < hasta &&
                    t.Estado != EstadoTurno.Cancelado &&
                    t.Estado != EstadoTurno.ClienteAusente)
                .OrderBy(t => t.FechaInicio)
                .ToListAsync();

            return ServiceResult<List<Turno>>
                .Ok(turnos);
        }


        // =====================================
        // VALIDAR DISPONIBILIDAD
        // =====================================

        internal async Task<ServiceResult>
            ValidarDisponibilidadAsync(
                DateTime fechaInicio)
        {
            return await ValidarDisponibilidadAsync(
                fechaInicio,
                null);
        }


        internal async Task<ServiceResult>
            ValidarDisponibilidadAsync(
                DateTime fechaInicio,
                int turnoIdExcluir)
        {
            return await ValidarDisponibilidadAsync(
                fechaInicio,
                (int?)turnoIdExcluir);
        }


        private async Task<ServiceResult>
            ValidarDisponibilidadAsync(
                DateTime fechaInicio,
                int? turnoIdExcluir)
        {
            // ---------------------------------
            // FECHA FUTURA
            // ---------------------------------

            if (fechaInicio <= DateTime.Now)
            {
                return ServiceResult.Error(
                    "El turno debe corresponder a una fecha futura.");
            }


            // ---------------------------------
            // CONFIGURACIÓN
            // ---------------------------------

            var turnosPorFranja =
                ObtenerTurnosPorFranja();

            var duracionFranja =
                ObtenerDuracionFranjaMinutos();

            var horaApertura =
                ObtenerHoraApertura();

            var horaCierre =
                ObtenerHoraCierre();

            if (turnosPorFranja <= 0)
            {
                return ServiceResult.Error(
                    "La cantidad de turnos por franja no está configurada correctamente.");
            }

            if (duracionFranja <= 0)
            {
                return ServiceResult.Error(
                    "La duración de la franja de turnos no está configurada correctamente.");
            }

            if (horaApertura >= horaCierre)
            {
                return ServiceResult.Error(
                    "El horario del taller no está configurado correctamente.");
            }


            // ---------------------------------
            // HORARIO DEL TALLER
            // ---------------------------------

            var apertura =
                fechaInicio.Date.Add(
                    horaApertura);

            var cierre =
                fechaInicio.Date.Add(
                    horaCierre);

            if (fechaInicio < apertura ||
                fechaInicio >= cierre)
            {
                return ServiceResult.Error(
                    $"El horario del taller es de " +
                    $"{apertura:HH:mm} a " +
                    $"{cierre:HH:mm}.");
            }


            // ---------------------------------
            // VALIDAR QUE RESPETE LA FRANJA
            // ---------------------------------

            var minutosDesdeApertura =
                (fechaInicio - apertura)
                .TotalMinutes;

            if (minutosDesdeApertura %
                duracionFranja != 0)
            {
                return ServiceResult.Error(
                    $"Los turnos deben asignarse cada " +
                    $"{duracionFranja} minutos.");
            }


            // ---------------------------------
            // CONTAR TURNOS DE ESA FRANJA
            // ---------------------------------

            var query =
                _context.Turnos
                    .Where(t =>
                        t.FechaInicio ==
                            fechaInicio &&
                        t.Estado !=
                            EstadoTurno.Cancelado &&
                        t.Estado !=
                            EstadoTurno.ClienteAusente);

            if (turnoIdExcluir.HasValue)
            {
                query = query.Where(t =>
                    t.Id !=
                    turnoIdExcluir.Value);
            }

            var cantidadTurnos =
                await query.CountAsync();

            if (cantidadTurnos >=
                turnosPorFranja)
            {
                return ServiceResult.Error(
                    "No hay disponibilidad para ese horario.");
            }

            return ServiceResult.Ok(
                "Hay disponibilidad para ese horario.");
        }


        // =====================================
        // HORARIOS DISPONIBLES
        // =====================================

        public async Task<ServiceResult<List<DateTime>>>
            ObtenerHorariosDisponiblesAsync(
                DateTime fecha, int usuarioSolicitanteId)
        {
            if (!await _permisos.TieneAlgunoAsync(usuarioSolicitanteId, "TURNO_VER", "TURNO_CREAR", "TURNO_MODIFICAR", "CLIENTE_TURNO_CREAR")) return ServiceResult<List<DateTime>>.Error("Acceso denegado.");

            var turnosPorFranja =
                ObtenerTurnosPorFranja();

            var duracionFranja =
                ObtenerDuracionFranjaMinutos();

            var horaApertura =
                ObtenerHoraApertura();

            var horaCierre =
                ObtenerHoraCierre();

            if (turnosPorFranja <= 0)
            {
                return ServiceResult<List<DateTime>>
                    .Error(
                        "La cantidad de turnos por franja no está configurada correctamente.");
            }

            if (duracionFranja <= 0)
            {
                return ServiceResult<List<DateTime>>
                    .Error(
                        "La duración de la franja no está configurada correctamente.");
            }

            if (horaApertura >= horaCierre)
            {
                return ServiceResult<List<DateTime>>
                    .Error(
                        "El horario del taller no está configurado correctamente.");
            }


            var apertura =
                fecha.Date.Add(
                    horaApertura);

            var cierre =
                fecha.Date.Add(
                    horaCierre);


            // ---------------------------------
            // TURNOS DEL DÍA
            // ---------------------------------

            var turnos = await _context.Turnos
                .Where(t =>
                    t.FechaInicio >= apertura &&
                    t.FechaInicio < cierre &&
                    t.Estado !=
                        EstadoTurno.Cancelado &&
                    t.Estado !=
                        EstadoTurno.ClienteAusente)
                .ToListAsync();


            // ---------------------------------
            // GENERAR FRANJAS
            // ---------------------------------

            var horarios =
                new List<DateTime>();

            for (
                var horario = apertura;
                horario < cierre;
                horario =
                    horario.AddMinutes(
                        duracionFranja))
            {
                // No ofrecer horarios
                // que ya pasaron.

                if (horario <= DateTime.Now)
                {
                    continue;
                }


                var cantidad =
                    turnos.Count(t =>
                        t.FechaInicio ==
                        horario);

                if (cantidad <
                    turnosPorFranja)
                {
                    horarios.Add(
                        horario);
                }
            }

            return ServiceResult<List<DateTime>>
                .Ok(horarios);
        }


        // =====================================
        // AGENDA DEL DÍA
        // =====================================

        public async Task<ServiceResult<List<Turno>>>
            ObtenerAgendaDelDiaAsync(
                DateTime fecha, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "TURNO_VER")) return ServiceResult<List<Turno>>.Error("Acceso denegado.");

            var desde =
                fecha.Date;

            var hasta =
                desde.AddDays(1);

            return await
                ObtenerTurnosDelPeriodoAsync(
                    desde,
                    hasta, usuarioSolicitanteId);
        }
    }
}