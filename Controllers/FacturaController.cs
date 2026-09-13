using System.Security.Claims;
using MecaniCar360.Attributes;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class FacturaController : Controller
    {
        private readonly FacturaService _facturaService;
        public FacturaController(FacturaService facturaService) => _facturaService = facturaService;

        [HttpGet("/Factura")]
        [HttpGet("/Factura/Index")]
        [Permiso("FACTURA_VER")]
        public async Task<IActionResult> Index()
        {
            if (UsuarioId() is not int usuarioId) return Forbid();
            var resultado = await _facturaService.ListarAsync(usuarioId);
            return resultado.Exitoso ? View(resultado.Data) : Forbid();
        }

        [HttpGet]
        [Permiso("CLIENTE_FACTURA_VER")]
        public async Task<IActionResult> MisFacturas()
        {
            if (UsuarioId() is not int usuarioId) return Forbid();
            var resultado = await _facturaService.MisFacturasAsync(usuarioId);
            return resultado.Exitoso ? View(resultado.Data) : Forbid();
        }

        [HttpGet]
        [Permiso("FACTURA_VER")]
        public async Task<IActionResult> Detalle(int ordenTrabajoId)
        {
            if (UsuarioId() is not int usuarioId) return Forbid();
            return Mostrar(await _facturaService.ObtenerAsync(ordenTrabajoId, usuarioId), "Detalle");
        }

        [HttpGet]
        [Permiso("FACTURA_VER")]
        public async Task<IActionResult> PorId(int facturaId)
        {
            if (UsuarioId() is not int usuarioId) return Forbid();
            return Mostrar(await _facturaService.ObtenerPorIdAsync(facturaId, usuarioId), "Detalle");
        }

        [HttpGet]
        [Permiso("CLIENTE_FACTURA_VER")]
        public async Task<IActionResult> DetallePropio(int facturaId)
        {
            if (UsuarioId() is not int usuarioId) return Forbid();
            ViewData["Propia"] = true;
            return Mostrar(await _facturaService.ObtenerPropiaAsync(facturaId, usuarioId), "Detalle");
        }

        [HttpGet]
        [Permiso("PAGO_VER")]
        public async Task<IActionResult> Pagos(int facturaId)
        {
            if (UsuarioId() is not int usuarioId) return Forbid();
            return Mostrar(await _facturaService.ObtenerPagosAsync(facturaId, usuarioId), "Pagos");
        }

        [HttpGet]
        [Permiso("CLIENTE_FACTURA_VER")]
        public async Task<IActionResult> PagosPropios(int facturaId)
        {
            if (UsuarioId() is not int usuarioId) return Forbid();
            ViewData["Propia"] = true;
            return Mostrar(await _facturaService.ObtenerPropiaAsync(facturaId, usuarioId), "Pagos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("FACTURA_CREAR")]
        public async Task<IActionResult> Emitir(int ordenTrabajoId)
        {
            if (UsuarioId() is not int usuarioId) return Forbid();
            if (!ModelState.IsValid) return BadRequest("Orden inválida.");
            var resultado = await _facturaService.EmitirAsync(ordenTrabajoId, usuarioId);
            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;
            return resultado.Exitoso && resultado.Data != null
                ? RedirectToAction(nameof(PorId), new { facturaId = resultado.Data.Id })
                : RedirectToAction("Detalle", "OrdenTrabajo", new { id = ordenTrabajoId });
        }

        [HttpGet]
        [Permiso("PAGO_REGISTRAR")]
        public async Task<IActionResult> RegistrarPago(int facturaId)
        {
            if (UsuarioId() is not int usuarioId) return Forbid();
            var resultado = await _facturaService.ObtenerParaPagoAsync(facturaId, usuarioId);
            if (!resultado.Exitoso || resultado.Data == null) return NotFound();
            var factura = resultado.Data;
            var saldo = factura.Total - factura.Pagos.Where(p => p.Estado == EstadoPago.Pagado).Sum(p => p.Monto);
            return View(new RegistrarPagoViewModel
            {
                FacturaId = factura.Id, Monto = saldo, SaldoEsperado = saldo, Factura = factura
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("PAGO_REGISTRAR")]
        public async Task<IActionResult> RegistrarPago(RegistrarPagoViewModel model)
        {
            if (UsuarioId() is not int usuarioId) return Forbid();
            if (ModelState.IsValid)
            {
                var resultado = await _facturaService.RegistrarPagoAsync(
                    model.FacturaId, model.Monto, model.MetodoPago, usuarioId, model.SaldoEsperado);
                if (resultado.Exitoso)
                {
                    TempData["Ok"] = resultado.Mensaje;
                    return RedirectToAction(nameof(RegistrarPago), new { facturaId = model.FacturaId });
                }
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
            }
            var consulta = await _facturaService.ObtenerParaPagoAsync(model.FacturaId, usuarioId);
            if (!consulta.Exitoso || consulta.Data == null) return NotFound();
            model.Factura = consulta.Data;
            // Mantener el saldo observado al enviar: un POST repetido no se vuelve a cobrar.
            // El enlace Actualizar permite revisar el saldo antes de un nuevo intento.
            return View(model);
        }

        private IActionResult Mostrar(ServiceResult<Factura> resultado, string vista) =>
            resultado.Exitoso && resultado.Data != null ? View(vista, resultado.Data) : NotFound();

        private int? UsuarioId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0
            ? id : null;
    }
}
