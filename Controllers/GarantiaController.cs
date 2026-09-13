using MecaniCar360.Attributes;
using MecaniCar360.Helpers;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class GarantiaController : Controller
    {
        private readonly GarantiaService _garantiaService;

        public GarantiaController(
            GarantiaService garantiaService)
        {
            _garantiaService = garantiaService;
        }


        // =====================================================
        // LISTAR GARANTÍAS
        //
        // ADMIN / CAJA
        // =====================================================

        [HttpGet]
        [Permiso("GARANTIA_VER")]
        public async Task<IActionResult> Index()
        {
            var resultado =
                await _garantiaService
                    .ObtenerTodasAsync(ObtenerUsuarioId() ?? 0);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            return View(resultado.Data);
        }


        // =====================================================
        // DETALLE
        //
        // ADMIN / CAJA / MECÁNICO
        // =====================================================

        [HttpGet]
        [Permiso("GARANTIA_VER")]
        public async Task<IActionResult> Detalle(
            int id)
        {
            var resultado =
                await _garantiaService
                    .ObtenerAsync(id, ObtenerUsuarioId() ?? 0);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Index));
            }

            return View(resultado.Data);
        }


        // =====================================================
        // MIS GARANTÍAS
        //
        // CLIENTE
        // =====================================================

        [HttpGet]
        [Permiso("GARANTIA_VER_PROPIA")]
        public async Task<IActionResult> MisGarantias()
        {
            var usuarioId =
                ObtenerUsuarioId();

            if (usuarioId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _garantiaService
                    .ObtenerPorClienteAsync(
                        usuarioId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            return View(resultado.Data);
        }


        // =====================================================
        // GARANTÍAS DE UN VEHÍCULO
        //
        // ADMIN / CAJA / MECÁNICO
        // =====================================================

        [HttpGet]
        [Permiso("GARANTIA_VER")]
        public async Task<IActionResult> PorVehiculo(
            int vehiculoId)
        {
            var resultado =
                await _garantiaService
                    .ObtenerPorVehiculoAsync(
                        vehiculoId, ObtenerUsuarioId() ?? 0);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Index));
            }

            return View(
                "PorVehiculo",
                resultado.Data);
        }


        // =====================================================
        // CREAR GARANTÍA
        //
        // ADMIN / CAJA
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("GARANTIA_CREAR")]
        public async Task<IActionResult> Crear(
            int ordenTrabajoId,
            List<GarantiaItemDto> items)
        {
            var usuarioId =
                ObtenerUsuarioId();

            if (usuarioId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            if (ordenTrabajoId <= 0 || !ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado =
                await _garantiaService
                    .CrearAsync(
                        ordenTrabajoId,
                        usuarioId.Value,
                        items);

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
                "Garantía generada correctamente.";

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id = resultado.Data!.Id
                });
        }


        // =====================================================
        // VERIFICAR GARANTÍA VIGENTE
        // =====================================================

        [HttpGet]
        [Permiso("GARANTIA_VER")]
        public async Task<JsonResult>
            EstaVigente(
                int id)
        {
            var resultado =
                await _garantiaService
                    .EstaVigenteAsync(id, ObtenerUsuarioId() ?? 0);

            return Json(new
            {
                exitoso =
                    resultado.Exitoso,

                mensaje =
                    resultado.Mensaje
            });
        }


        // =====================================================
        // VERIFICAR COBERTURA DE ÍTEM
        // =====================================================

        [HttpGet]
        [Permiso("GARANTIA_VER")]
        public async Task<JsonResult>
            ItemEstaCubierto(
                int id)
        {
            var resultado =
                await _garantiaService
                    .ItemEstaCubiertoAsync(id, ObtenerUsuarioId() ?? 0);

            return Json(new
            {
                exitoso =
                    resultado.Exitoso,

                mensaje =
                    resultado.Mensaje
            });
        }


        // =====================================================
        // ANULAR GARANTÍA
        //
        // SOLO ADMIN
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("GARANTIA_ANULAR")]
        public async Task<IActionResult> Anular(
            int id)
        {
            var resultado =
                await _garantiaService
                    .AnularAsync(id, ObtenerUsuarioId() ?? 0);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Detalle),
                    new
                    {
                        id
                    });
            }

            TempData["Ok"] =
                resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id
                });
        }


        // =====================================================
        // HELPERS
        // =====================================================

        [HttpGet, Permiso("GARANTIA_VER_PROPIA")]
        public async Task<IActionResult> Propia(int id)
        {
            var resultado = await _garantiaService.ObtenerPropiaAsync(id, ObtenerUsuarioId() ?? 0);
            if (!resultado.Exitoso) return NotFound();
            var g = resultado.Data!;
            return Json(new { g.Id, g.OrdenTrabajoId, g.FechaInicio, g.FechaFin, g.Activa });
        }

        private int? ObtenerUsuarioId()
        {
            var claim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(
                    claim))
            {
                return null;
            }

            if (int.TryParse(
                    claim,
                    out int usuarioId))
            {
                return usuarioId;
            }

            return null;
        }


    }
}
