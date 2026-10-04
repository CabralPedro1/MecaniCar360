using MecaniCar360.Models;
using MecaniCar360.Attributes;
using MecaniCar360.Services;
using MecaniCar360.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class IngresoVehiculoController : Controller
    {
        private readonly IngresoVehiculoService _ingresoService;

        public IngresoVehiculoController(
            IngresoVehiculoService ingresoService)
        {
            _ingresoService = ingresoService;
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
            var usuarioId = ObtenerUsuarioId();
            var resultado =
                await _ingresoService
                    .PrepararRegistroAsync(turnoId, usuarioId);

            if (!resultado.Exitoso || resultado.Data == null)
            {
                TempData["Error"] = resultado.Mensaje;
                return RedirectToAction("Detalle", "Turno", new { id = turnoId });
            }

            if (resultado.Data.Turno?.IngresoVehiculo != null)
            {
                return RedirectToAction(
                    nameof(Detalle),
                    new
                    {
                        id = resultado.Data.Turno.IngresoVehiculo.Id
                    });
            }

            return View(resultado.Data);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("INGRESO_REGISTRAR")]
        public async Task<IActionResult> Registrar(
            RegistrarIngresoViewModel model)
        {
            var usuarioId =
                ObtenerUsuarioId();

            if (!ModelState.IsValid)
            {
                var form = await _ingresoService.PrepararRegistroAsync(model.TurnoId, usuarioId);
                if (form.Exitoso && form.Data != null)
                {
                    model.Turno = form.Data.Turno;
                    model.Vehiculos = form.Data.Vehiculos;
                }
                return View(model);
            }

            var resultado =
                await _ingresoService
                    .RegistrarIngresoYCrearOrdenAsync(
                        model,
                        usuarioId);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                var form = await _ingresoService.PrepararRegistroAsync(model.TurnoId, usuarioId);
                if (form.Exitoso && form.Data != null)
                {
                    model.Turno = form.Data.Turno;
                    model.Vehiculos = form.Data.Vehiculos;
                }
                return View(model);
            }

            TempData["Ok"] =
                resultado.Mensaje;

            // Después del ingreso,
            // vamos directamente a la OT.

            return RedirectToAction(
                "Detalle",
                "OrdenTrabajo",
                new
                {
                    id =
                        resultado.Data!.Id
                });
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