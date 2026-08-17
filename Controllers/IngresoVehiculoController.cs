using MecaniCar360.Models;
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

        public IngresoVehiculoController(
            IngresoVehiculoService ingresoService)
        {
            _ingresoService = ingresoService;
        }


        // =====================================
        // DETALLE DEL INGRESO
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var resultado =
                await _ingresoService
                    .ObtenerPorIdAsync(id);

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
        public async Task<IActionResult> PorTurno(
            int turnoId)
        {
            var resultado =
                await _ingresoService
                    .ObtenerPorTurnoAsync(
                        turnoId);

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
        public async Task<IActionResult> Registrar(
            int turnoId)
        {
            var resultado =
                await _ingresoService
                    .ObtenerPorTurnoAsync(
                        turnoId);

            if (resultado.Exitoso)
            {
                return RedirectToAction(
                    nameof(Detalle),
                    new
                    {
                        id =
                            resultado.Data!.Id
                    });
            }

            ViewBag.TurnoId =
                turnoId;

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(
            int turnoId,
            bool clienteEspera,
            string? observaciones)
        {
            var usuarioId =
                ObtenerUsuarioId();

            var resultado =
                await _ingresoService
                    .RegistrarIngresoYCrearOrdenAsync(
                        turnoId,
                        clienteEspera,
                        observaciones,
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
        public async Task<IActionResult> EnTaller()
        {
            var resultado =
                await _ingresoService
                    .ObtenerVehiculosEnTallerAsync();

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
        // REGISTRAR EGRESO
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEgreso(
            int id,
            string? observaciones)
        {
            var resultado =
                await _ingresoService
                    .RegistrarEgresoAsync(
                        id,
                        observaciones);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id
                });
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