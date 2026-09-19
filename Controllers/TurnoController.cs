using MecaniCar360.Models;
using MecaniCar360.Models.Enums;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Attributes;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class TurnoController : Controller
    {
        private readonly TurnoService _turnoService;
        private readonly AgendaService _agendaService;
        private readonly PersonaService _personaService;
        private readonly DominioVehicularService _dominioService;

        public TurnoController(
            TurnoService turnoService,
            AgendaService agendaService,
            PersonaService personaService,
            DominioVehicularService dominioService)
        {
            _turnoService = turnoService;
            _agendaService = agendaService;
            _personaService = personaService;
            _dominioService = dominioService;
        }

        // =====================================
        // INDEX
        // =====================================

        [Permiso("TURNO_VER")]
        public async Task<IActionResult> Index()
        {
            var resultado = await _turnoService
                .ObtenerTodosAsync(ObtenerUsuarioId());

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

        [Permiso("TURNO_VER")]
        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _turnoService
                .ObtenerPorIdAsync(id, ObtenerUsuarioId());

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }


        // =====================================
        // AGENDA
        // =====================================

        [Permiso("TURNO_VER")]
        public async Task<IActionResult> Agenda(DateTime? fecha)
        {
            if (!ModelState.IsValid || fecha?.Date == DateTime.MaxValue.Date)
                return BadRequest("Fecha inválida.");
            var dia = fecha?.Date ?? DateTime.Today;

            var resultado =
                await _agendaService.ObtenerAgendaDelDiaAsync(dia, SolicitanteId());

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
        [Permiso("TURNO_CREAR")]
        public async Task<IActionResult> Crear(
            int? clienteId = null,
            int? vehiculoId = null)
        {
            if (!ModelState.IsValid || clienteId <= 0 || vehiculoId <= 0)
                return BadRequest("Contexto de turno inválido.");
            var model = new CrearTurnoViewModel
            {
                ClienteId = clienteId ?? 0,
                VehiculoId = vehiculoId ?? 0,

                // Por defecto proponemos mañana.
                FechaInicio = DateTime.Today.AddDays(1)
                    .AddHours(8)
            };

            await CargarSelectoresAsync(model);
            return View(model);
        }


        // =====================================
        // CREAR - POST
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("TURNO_CREAR")]
        public async Task<IActionResult> Crear(
            CrearTurnoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarSelectoresAsync(model);
                return View(model);
            }

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

                await CargarSelectoresAsync(model);
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
        [Permiso("TURNO_CONFIRMAR")]
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
        [Permiso("TURNO_CANCELAR")]
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
        [Permiso("TURNO_MODIFICAR")]
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
        [Permiso("TURNO_MODIFICAR")]
        public async Task<IActionResult> Reprogramar(
            int id,
            DateTime nuevaFechaInicio)
        {
            if (!ModelState.IsValid || nuevaFechaInicio == default)
            {
                TempData["Error"] = "Seleccione una fecha y un horario válidos.";
                return RedirectToAction(nameof(Detalle), new { id });
            }
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
        [Permiso("TURNO_VER", "TURNO_CREAR", "TURNO_MODIFICAR", "CLIENTE_TURNO_CREAR")]
        public async Task<JsonResult> HorariosDisponibles(
     DateTime fecha)
        {
            if (!ModelState.IsValid || fecha == default || fecha.Date == DateTime.MaxValue.Date)
                return new JsonResult(new { exitoso = false, mensaje = "Fecha inválida." }) { StatusCode = 400 };
            var resultado =
                await _agendaService
                    .ObtenerHorariosDisponiblesAsync(
                        fecha, SolicitanteId());

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

        private async Task CargarSelectoresAsync(CrearTurnoViewModel model)
        {
            var personas = await _personaService.ObtenerActivasAsync(SolicitanteId());
            if (!personas.Exitoso) ModelState.AddModelError(string.Empty, personas.Mensaje);
            if (personas.Exitoso && model.ClienteId > 0 && !personas.Data!.Any(p => p.Id == model.ClienteId))
                ModelState.AddModelError(nameof(model.ClienteId), "Seleccione una persona activa disponible.");
            ViewBag.Clientes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                (personas.Data ?? new List<Persona>()).Select(p => new { p.Id, Nombre = p.Apellido + ", " + p.Nombre }),
                "Id", "Nombre", model.ClienteId);
            var vehiculos = new List<Vehiculo>();
            if (model.ClienteId > 0)
            {
                var resultado = await _dominioService.ObtenerVehiculosDePersonaAsync(model.ClienteId, SolicitanteId());
                if (!resultado.Exitoso) ModelState.AddModelError(string.Empty, resultado.Mensaje);
                else vehiculos = resultado.Data!;
            }
            if (model.VehiculoId > 0 && !vehiculos.Any(v => v.Id == model.VehiculoId))
                ModelState.AddModelError(nameof(model.VehiculoId), "Seleccione un vehículo disponible para esa persona.");
            ViewBag.Vehiculos = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(vehiculos, "Id", "Patente", model.VehiculoId);
        }

        [HttpGet, Permiso("TURNO_CREAR")]
        public async Task<IActionResult> VehiculosDeCliente(int clienteId)
        {
            if (!ModelState.IsValid || clienteId <= 0) return BadRequest("Cliente inválido.");
            var persona = await _personaService.ObtenerPorIdAsync(clienteId, SolicitanteId());
            if (!persona.Exitoso || persona.Data == null || !persona.Data.Activo) return NotFound();
            var resultado = await _dominioService.ObtenerVehiculosDePersonaAsync(clienteId, SolicitanteId());
            if (!resultado.Exitoso) return StatusCode(403, new { mensaje = "No se pudieron cargar los vehículos." });
            return Json(resultado.Data!.Select(v => new { id = v.Id, patente = v.Patente }));
        }

        private int ObtenerUsuarioId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el identificador del usuario en la sesión.");
            }

            return int.Parse(claim.Value);
        }
        [HttpGet, Permiso("CLIENTE_TURNO_VER")]
        public async Task<IActionResult> MisTurnos()
        {
            var resultado = await _turnoService.ObtenerTurnosPropiosAsync(SolicitanteId());
            if (!resultado.Exitoso) return Forbid();
            return Json(resultado.Data!.Select(t => new { t.Id, t.FechaInicio, t.Estado, t.Motivo, t.VehiculoId }));
        }

        [HttpGet, Permiso("CLIENTE_TURNO_VER")]
        public async Task<IActionResult> Propio(int id)
        {
            var resultado = await _turnoService.ObtenerTurnoPropioAsync(SolicitanteId(), id);
            if (!resultado.Exitoso) return NotFound();
            var t = resultado.Data!;
            return Json(new { t.Id, t.FechaInicio, t.Estado, t.Motivo, t.VehiculoId });
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("CLIENTE_TURNO_CREAR")]
        public async Task<IActionResult> CrearPropio(int vehiculoId, TipoTurno tipo, DateTime fechaInicio, string motivo, string? observaciones)
        {
            if (!ModelState.IsValid) return BadRequest("Datos inválidos.");
            var resultado = await _turnoService.CrearPropioAsync(SolicitanteId(), vehiculoId, tipo, fechaInicio, motivo, observaciones);
            return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado.Mensaje);
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("CLIENTE_TURNO_CANCELAR")]
        public async Task<IActionResult> CancelarPropio(int id, string? motivo)
        {
            var resultado = await _turnoService.CancelarPropioAsync(id, SolicitanteId(), motivo);
            return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado.Mensaje);
        }

        private int SolicitanteId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    }
}
