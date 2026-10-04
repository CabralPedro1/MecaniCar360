using MecaniCar360.Attributes;
using MecaniCar360.Helpers;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models;
using MecaniCar360.Models.ViewModels;
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

            return View(resultado.Data!.Select(ParaVista).ToList());
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

            return View(ParaVista(resultado.Data!));
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

            ViewData["Propia"] = true;
            return View(resultado.Data!.Select(ParaVista).ToList());
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
                resultado.Data!.Select(ParaVista).ToList());
        }


        // =====================================================
        // CREAR GARANTÍA
        //
        // ADMIN / CAJA
        // =====================================================

        [HttpGet, Permiso("GARANTIA_CREAR")]
        public async Task<IActionResult> Crear(int ordenTrabajoId)
        {
            var resultado = await _garantiaService.ObtenerParaCrearAsync(ordenTrabajoId, ObtenerUsuarioId() ?? 0);
            if (!resultado.Exitoso) ModelState.AddModelError(string.Empty, resultado.Mensaje);
            return View(resultado.Data ?? new CrearGarantiaViewModel { OrdenTrabajoId = ordenTrabajoId });
        }

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
                    nameof(Crear),
                    new
                    {
                        ordenTrabajoId
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
        public async Task<IActionResult> DetallePropio(int id)
        {
            var resultado = await _garantiaService.ObtenerPropiaAsync(id, ObtenerUsuarioId() ?? 0);
            if (!resultado.Exitoso) return NotFound();
            ViewData["Propia"] = true;
            return View("Detalle", ParaVista(resultado.Data!));
        }

        [HttpGet, Permiso("GARANTIA_VER_PROPIA"), Permiso("CLIENTE_VEHICULO_VER")]
        public async Task<IActionResult> PorVehiculoPropio(int vehiculoId)
        {
            var resultado = await _garantiaService.ObtenerPorVehiculoPropioAsync(vehiculoId, ObtenerUsuarioId() ?? 0);
            if (!resultado.Exitoso) return NotFound();
            ViewData["Propia"] = true;
            ViewData["VehiculoId"] = vehiculoId;
            return View("PorVehiculo", resultado.Data!.Select(ParaVista).ToList());
        }

        private static GarantiaDetalleViewModel ParaVista(Garantia garantia) => new()
        { Garantia = garantia, Estado = GarantiaService.DescribirVigencia(garantia) };

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
