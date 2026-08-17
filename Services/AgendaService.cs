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

        public AgendaService(MecaniCarContext context)
        {
            _context = context;
        }

        // =====================================
        // BOXES
        // =====================================

        private async Task<int> ObtenerCantidadBoxesActivosAsync()
        {
            return await _context.BoxesTrabajo
                .CountAsync(b => b.Activo);
        }


        // =====================================
        // TURNOS DEL PERÍODO
        // =====================================

        public async Task<ServiceResult<List<Turno>>> ObtenerTurnosDelPeriodoAsync(
            DateTime desde,
            DateTime hasta)
        {
            var turnos = await _context.Turnos
                .Include(t => t.Vehiculo)
                .Include(t => t.Cliente)
                .Where(t =>
                    t.FechaInicio < hasta &&
                    t.FechaInicio.Add(t.DuracionEstimada) > desde &&
                    t.Estado != EstadoTurno.Cancelado &&
                    t.Estado != EstadoTurno.ClienteAusente)
                .OrderBy(t => t.FechaInicio)
                .ToListAsync();

            return ServiceResult<List<Turno>>.Ok(turnos);
        }


        // =====================================
        // VALIDAR DISPONIBILIDAD
        // =====================================

        public async Task<ServiceResult> ValidarDisponibilidadAsync(
            DateTime fechaInicio,
            TimeSpan duracion)
        {
            if (duracion <= TimeSpan.Zero)
                return ServiceResult.Error(
                    "La duración del turno debe ser mayor a cero.");

            var cantidadBoxes = await ObtenerCantidadBoxesActivosAsync();

            if (cantidadBoxes <= 0)
                return ServiceResult.Error(
                    "No existen boxes de trabajo activos.");

            var fechaFin = fechaInicio.Add(duracion);

            var turnos = await _context.Turnos
                .Where(t =>
                    t.FechaInicio < fechaFin &&
                    t.FechaInicio.Add(t.DuracionEstimada) > fechaInicio &&
                    t.Estado != EstadoTurno.Cancelado &&
                    t.Estado != EstadoTurno.ClienteAusente)
                .ToListAsync();

            if (turnos.Count >= cantidadBoxes)
            {
                return ServiceResult.Error(
                    "No hay capacidad disponible para ese horario.");
            }

            return ServiceResult.Ok(
                "Hay capacidad disponible.");
        }


        // =====================================
        // HORARIOS DISPONIBLES
        // =====================================

        public async Task<ServiceResult<List<DateTime>>> ObtenerHorariosDisponiblesAsync(
            DateTime fecha,
            TimeSpan duracion,
            TimeSpan apertura,
            TimeSpan cierre,
            TimeSpan intervalo)
        {
            if (duracion <= TimeSpan.Zero)
                return ServiceResult<List<DateTime>>.Error(
                    "La duración debe ser mayor a cero.");

            if (intervalo <= TimeSpan.Zero)
                return ServiceResult<List<DateTime>>.Error(
                    "El intervalo debe ser mayor a cero.");

            if (apertura >= cierre)
                return ServiceResult<List<DateTime>>.Error(
                    "El horario de apertura debe ser anterior al cierre.");

            var cantidadBoxes = await ObtenerCantidadBoxesActivosAsync();

            if (cantidadBoxes <= 0)
                return ServiceResult<List<DateTime>>.Error(
                    "No existen boxes de trabajo activos.");

            var inicio = fecha.Date.Add(apertura);
            var limite = fecha.Date.Add(cierre);

            var turnos = await _context.Turnos
                .Where(t =>
                    t.FechaInicio < limite &&
                    t.FechaInicio.Add(t.DuracionEstimada) > inicio &&
                    t.Estado != EstadoTurno.Cancelado &&
                    t.Estado != EstadoTurno.ClienteAusente)
                .ToListAsync();

            var horariosDisponibles = new List<DateTime>();

            for (
                var horario = inicio;
                horario.Add(duracion) <= limite;
                horario = horario.Add(intervalo))
            {
                var fin = horario.Add(duracion);

                var cantidadSuperpuesta = turnos.Count(t =>
                    t.FechaInicio < fin &&
                    t.FechaInicio.Add(t.DuracionEstimada) > horario);

                if (cantidadSuperpuesta < cantidadBoxes)
                {
                    horariosDisponibles.Add(horario);
                }
            }

            return ServiceResult<List<DateTime>>.Ok(
                horariosDisponibles);
        }


        // =====================================
        // AGENDA DEL DÍA
        // =====================================

        public async Task<ServiceResult<List<Turno>>> ObtenerAgendaDelDiaAsync(
            DateTime fecha)
        {
            var desde = fecha.Date;
            var hasta = desde.AddDays(1);

            return await ObtenerTurnosDelPeriodoAsync(
                desde,
                hasta);
        }
    }
}