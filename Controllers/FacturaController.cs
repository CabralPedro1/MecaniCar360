using MecaniCar360.Models.Enums;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class FacturaController : Controller
    {
        private readonly FacturaService _facturaService;

        public FacturaController(
            FacturaService facturaService)
        {
            _facturaService = facturaService;
        }


        // =====================================
        // VER FACTURA DE UNA ORDEN
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Detalle(
            int ordenTrabajoId)
        {
            var resultado =
                await _facturaService
                    .ObtenerAsync(ordenTrabajoId);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    "Index",
                    "OrdenTrabajo");
            }

            return View(resultado.Data);
        }


        // =====================================
        // REGISTRAR PAGO
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPago(
            int ordenTrabajoId,
            MetodoPago metodoPago)
        {
            var claim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (claim == null)
            {
                TempData["Error"] =
                    "No se pudo identificar al usuario actual.";

                return RedirectToAction(
                    "Index",
                    "OrdenTrabajo");
            }

            if (!int.TryParse(
                claim,
                out int usuarioId))
            {
                TempData["Error"] =
                    "El usuario actual no es válido.";

                return RedirectToAction(
                    "Index",
                    "OrdenTrabajo");
            }


            var resultado =
                await _facturaService
                    .RegistrarPagoAsync(
                        ordenTrabajoId,
                        metodoPago,
                        usuarioId);


            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    "Detalle",
                    "OrdenTrabajo",
                    new
                    {
                        id = ordenTrabajoId
                    });
            }


            TempData["Ok"] =
                resultado.Mensaje;

            return RedirectToAction(
                "Detalle",
                "OrdenTrabajo",
                new
                {
                    id = ordenTrabajoId
                });
        }

        // =====================================
        // VER PAGOS DE UNA FACTURA
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Pagos(
            int facturaId)
        {
            var resultado =
                await _facturaService
                    .ObtenerPagosAsync(
                        facturaId);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    "Index",
                    "OrdenTrabajo");
            }

            return View(resultado.Data);
        }
    }
}