using MecaniCar360.Models;
using MecaniCar360.Models.Enums;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class TurnoController : Controller
    {
        private readonly TurnoService _turnoService;
        private readonly AgendaService _agendaService;

        public TurnoController(
            TurnoService turnoService,
            AgendaService agendaService)
        {
            _turnoService = turnoService;
            _agendaService = agendaService;
        }

        // =====================================
        // INDEX
        // =====================================

        public async Task<IActionResult> Index()
        {
            var resultado = await _turnoService.ObtenerTodosAsync();

            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;

                return View(new List<Turno>());
            }

            return View(resultado.Data);
        }


        // =====================================
        // DETALLE
        // =====================================

        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _turnoService.ObtenerPorIdAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }


        // =====================================
        // AGENDA
        // =====================================

        public async Task<IActionResult> Agenda(DateTime? fecha)
        {
            var dia = fecha?.Date ?? DateTime.Today;

            var resultado =
                await _agendaService.ObtenerAgendaDelDiaAsync(dia);

            var model = new AgendaViewModel
            {
                Fecha = dia,
                Turnos = resultado.Exitoso
                    ? resultado.Data ?? new List<Turno>()
                    : new List<Turno>()
            };

            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;
            }

            return View(model);
        }


        // =====================================
        // CREAR - GET
        // =====================================

        [HttpGet]
        public IActionResult Crear(
            int? clienteId = null,
            int? vehiculoId = null)
        {
            var model = new CrearTurnoViewModel
            {
                ClienteId = clienteId ?? 0,
                VehiculoId = vehiculoId ?? 0,

                // Por defecto proponemos mañana.
                FechaInicio = DateTime.Today.AddDays(1)
                    .AddHours(8)
            };

            return View(model);
        }


        // =====================================
        // CREAR - POST
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            CrearTurnoViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ---------------------------------
            // USUARIO ACTUAL
            // ---------------------------------

            var usuarioId = ObtenerUsuarioId();

            // ---------------------------------
            // CREAR TURNO
            // ---------------------------------

            var resultado = await _turnoService.CrearAsync(
                model.ClienteId,
                model.VehiculoId,
                model.Tipo,
                model.FechaInicio,
                model.Motivo,
                usuarioId,
                model.Observaciones);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(
                    string.Empty,
                    resultado.Mensaje);

                return View(model);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }


        // =====================================
        // CONFIRMAR
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(int id)
        {
            var resultado =
                await _turnoService.ConfirmarAsync(
                    id,
                    ObtenerUsuarioId());

            TempData[
                resultado.Exitoso ? "Ok" : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new { id });
        }


        // =====================================
        // CANCELAR
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(
            int id,
            string? motivo)
        {
            var resultado =
                await _turnoService.CancelarAsync(
                    id,
                    ObtenerUsuarioId(),
                    motivo);

            TempData[
                resultado.Exitoso ? "Ok" : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new { id });
        }


        // =====================================
        // CLIENTE AUSENTE
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClienteAusente(
            int id)
        {
            var resultado =
                await _turnoService.MarcarClienteAusenteAsync(
                    id,
                    ObtenerUsuarioId());

            TempData[
                resultado.Exitoso ? "Ok" : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new { id });
        }


        // =====================================
        // REPROGRAMAR
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reprogramar(
            int id,
            DateTime nuevaFechaInicio)
        {
            var resultado =
                await _turnoService.ReprogramarAsync(
                    id,
                    nuevaFechaInicio,
                    ObtenerUsuarioId());

            TempData[
                resultado.Exitoso ? "Ok" : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new { id });
        }


        // =====================================
        // HORARIOS DISPONIBLES
        // =====================================

        [HttpGet]
        public async Task<JsonResult> HorariosDisponibles(
     DateTime fecha)
        {
            var resultado =
                await _agendaService
                    .ObtenerHorariosDisponiblesAsync(
                        fecha);

            if (!resultado.Exitoso)
            {
                return Json(new
                {
                    exitoso = false,
                    mensaje = resultado.Mensaje,
                    horarios = new List<object>()
                });
            }

            return Json(new
            {
                exitoso = true,

                horarios = resultado.Data!
                    .Select(h => new
                    {
                        fecha = h.ToString("yyyy-MM-dd"),
                        hora = h.ToString("HH:mm"),
                        valor = h.ToString(
                            "yyyy-MM-ddTHH:mm")
                    })
                    .ToList()
            });
        }


        // =====================================
        // MÉTODOS PRIVADOS
        // =====================================

        private int ObtenerUsuarioId()
        {
            var claim = User.FindFirst("UsuarioId");

            if (claim == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el UsuarioId en la sesión.");
            }

            return int.Parse(claim.Value);
        }
    }
}