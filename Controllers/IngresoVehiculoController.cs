using MecaniCar360.Models;
using MecaniCar360.Attributes;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class IngresoVehiculoController : Controller
    {
        private readonly IngresoVehiculoService _ingresoService;
        private readonly TurnoService _turnoService;
        private readonly PermisoService _permisos;

        public IngresoVehiculoController(
            IngresoVehiculoService ingresoService,
            TurnoService turnoService,
            PermisoService permisos)
        {
            _ingresoService = ingresoService;
            _turnoService = turnoService;
            _permisos = permisos;
        }


        // =====================================
        // DETALLE DEL INGRESO
        // =====================================

        [HttpGet]
        [Permiso("INGRESO_VER")]
        public async Task<IActionResult> Detalle(int id)
        {
            var usuarioId = ObtenerUsuarioId();
            var resultado =
                await _ingresoService
                    .ObtenerPorIdAsync(id, usuarioId);

            if (!resultado.Exitoso)
            {
                return NotFound();
            }

            return View(resultado.Data);
        }


        // =====================================
        // INGRESO ASOCIADO A UN TURNO
        // =====================================

        [HttpGet]
        [Permiso("INGRESO_VER")]
        public async Task<IActionResult> PorTurno(
            int turnoId)
        {
            var usuarioId = ObtenerUsuarioId();
            var resultado =
                await _ingresoService
                    .ObtenerPorTurnoAsync(
                        turnoId,
                        usuarioId);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    "Detalle",
                    "Turno",
                    new
                    {
                        id = turnoId
                    });
            }

            return View(
                "Detalle",
                resultado.Data);
        }


        // =====================================
        // REGISTRAR INGRESO
        // =====================================

        [HttpGet]
        [Permiso("INGRESO_REGISTRAR")]
        public async Task<IActionResult> Registrar(
            int turnoId)
        {
            if (!ModelState.IsValid || turnoId <= 0) return BadRequest("Turno inválido.");
            return await MostrarRegistroAsync(turnoId);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("INGRESO_REGISTRAR")]
        public async Task<IActionResult> Registrar(
            int turnoId,
            bool clienteEspera,
            string? observaciones)
        {
            var usuarioId =
                ObtenerUsuarioId();

            if (turnoId <= 0) return BadRequest("Turno inválido.");
            ViewData["ClienteEspera"] = clienteEspera;
            ViewData["Observaciones"] = observaciones;
            if (!ModelState.IsValid) return await MostrarRegistroAsync(turnoId);

            var resultado =
                await _ingresoService
                    .RegistrarIngresoYCrearOrdenAsync(
                        turnoId,
                        clienteEspera,
                        observaciones,
                        usuarioId);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                return await MostrarRegistroAsync(turnoId);
            }

            TempData["Ok"] =
                resultado.Mensaje;

            if (await _permisos.TienePermisoAsync(usuarioId, "INGRESO_VER"))
                return RedirectToAction(nameof(Detalle), new { id = resultado.Data!.IngresoVehiculoId });
            // Registrar no concede implícitamente acceso de lectura al ingreso ni a la OT.
            return Content(resultado.Mensaje);
        }

        private async Task<IActionResult> MostrarRegistroAsync(int turnoId)
        {
            var usuarioId = ObtenerUsuarioId();
            if (!await _permisos.TienePermisoAsync(usuarioId, "TURNO_VER")) return Forbid();
            var turno = await _turnoService.ObtenerPorIdAsync(turnoId, usuarioId);
            if (!turno.Exitoso || turno.Data == null) return NotFound();
            return View("Registrar", turno.Data);
        }


        // =====================================
        // VEHÍCULOS EN EL TALLER
        // =====================================

        [HttpGet]
        [Permiso("INGRESO_VER")]
        public async Task<IActionResult> EnTaller()
        {
            var usuarioId = ObtenerUsuarioId();
            var resultado =
                await _ingresoService
                    .ObtenerVehiculosEnTallerAsync(
                        usuarioId);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return View(
                    new List<IngresoVehiculo>());
            }

            return View(
                resultado.Data);
        }


        // =====================================
        // USUARIO ACTUAL
        // =====================================

        private int ObtenerUsuarioId()
        {
            var claim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (claim == null)
            {
                throw new InvalidOperationException(
                    "No se pudo identificar al usuario actual.");
            }

            if (!int.TryParse(
                claim,
                out int usuarioId))
            {
                throw new InvalidOperationException(
                    "El identificador del usuario actual no es válido.");
            }

            return usuarioId;
        }
    }
}
