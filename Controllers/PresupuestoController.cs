using MecaniCar360.Attributes;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class PresupuestoController : Controller
    {
        private readonly PresupuestoService _service;
        private readonly StockService _stock;
        public PresupuestoController(PresupuestoService service, StockService stock)
        {
            _service = service;
            _stock = stock;
        }

        [HttpGet, Permiso("PRESUPUESTO_VER")]
        public async Task<IActionResult> Detalle(int ordenTrabajoId)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            var resultado = await _service.ObtenerAsync(ordenTrabajoId, usuario);
            if (!resultado.Exitoso) return BadRequest(resultado.Mensaje);
            var repuestos = await _stock.ObtenerRepuestosAsync(usuario);
            ViewData["Repuestos"] = repuestos.Data?.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = r.Id.ToString(), Text = r.Nombre
            }).ToList() ?? new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
            return View(resultado.Data);
        }

        [HttpGet, Permiso("PRESUPUESTO_VER")]
        public async Task<IActionResult> Obtener(int ordenTrabajoId)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            var resultado = await _service.ObtenerAsync(ordenTrabajoId, usuario);
            if (!resultado.Exitoso) return Json(new { exitoso = false, mensaje = resultado.Mensaje });
            var p = resultado.Data!;
            return Json(new
            {
                exitoso = true,
                presupuesto = new { id = p.Id, estado = p.Estado.ToString(), total = p.Total,
                    fechaUltimaModificacion = p.FechaUltimaModificacion, motivoRechazo = p.MotivoRechazo },
                items = p.Items.Select(i => new { id = i.Id, descripcion = i.Descripcion,
                    cantidad = i.Cantidad, precioUnitario = i.PrecioUnitario, subtotal = i.Subtotal, repuestoId = i.RepuestoId }),
                historial = p.Historial.Select(h => new { totalAnterior = h.TotalAnterior, fecha = h.Fecha, motivo = h.Motivo }),
                versiones = p.Versiones.OrderByDescending(v => v.NumeroVersion).Select(Snapshot)
            });
        }

        [HttpGet, Permiso("CLIENTE_PRESUPUESTO_VER")]
        public async Task<IActionResult> Propio(int ordenTrabajoId)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            var resultado = await _service.ObtenerPropioAsync(ordenTrabajoId, usuario);
            if (!resultado.Exitoso) return BadRequest(resultado.Mensaje);
            ViewData["PuedeDecidir"] = PuedeDecidir(resultado.Data!);
            return View(resultado.Data);
        }

        [HttpGet, Permiso("CLIENTE_PRESUPUESTO_VER")]
        public async Task<IActionResult> ObtenerPropio(int ordenTrabajoId)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            var resultado = await _service.ObtenerPropioAsync(ordenTrabajoId, usuario);
            return resultado.Exitoso
                ? Json(new { exitoso = true, version = Snapshot(resultado.Data!), puedeDecidir = PuedeDecidir(resultado.Data!) })
                : Json(new { exitoso = false, mensaje = resultado.Mensaje });
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("PRESUPUESTO_CREAR")]
        public async Task<IActionResult> Crear(int ordenTrabajoId)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            if (!ModelState.IsValid) return BadRequest("Datos inválidos.");
            return Resultado(await _service.CrearAsync(ordenTrabajoId, usuario));
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("PRESUPUESTO_MODIFICAR")]
        public async Task<IActionResult> AgregarItem(int presupuestoId, string descripcion,
            int cantidad, decimal precioUnitario, int? repuestoId)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            if (!ModelState.IsValid) return BadRequest("Datos del ítem inválidos.");
            return Resultado(await _service.AgregarItemAsync(presupuestoId, usuario, descripcion, cantidad, precioUnitario, repuestoId));
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("PRESUPUESTO_MODIFICAR")]
        public async Task<IActionResult> EliminarItem(int presupuestoId, int itemId)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            if (!ModelState.IsValid) return BadRequest("Datos inválidos.");
            return Resultado(await _service.EliminarItemAsync(presupuestoId, itemId, usuario));
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("PRESUPUESTO_ENVIAR")]
        public async Task<IActionResult> EnviarAprobacion(int presupuestoId, IEnumerable<int>? evidenciaIds = null)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            if (!ModelState.IsValid) return BadRequest("Datos de envío inválidos.");
            return Resultado(await _service.EnviarAprobacionAsync(presupuestoId, usuario, evidenciaIds));
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("CLIENTE_PRESUPUESTO_APROBAR")]
        public async Task<IActionResult> Aprobar(int presupuestoVersionId)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            if (!ModelState.IsValid) return BadRequest("Versión inválida.");
            return Resultado(await _service.AprobarAsync(presupuestoVersionId, usuario), true);
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("CLIENTE_PRESUPUESTO_RECHAZAR")]
        public async Task<IActionResult> Rechazar(int presupuestoVersionId, string motivo)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            if (!ModelState.IsValid) return BadRequest("Versión o motivo inválidos.");
            return Resultado(await _service.RechazarAsync(presupuestoVersionId, usuario, motivo), true);
        }

        private IActionResult Resultado(ServiceResult<PresupuestoOperacion> resultado, bool cliente = false)
        {
            if (!resultado.Exitoso) return BadRequest(resultado.Mensaje);
            TempData["Ok"] = resultado.Mensaje;
            return RedirectToAction(cliente ? nameof(Propio) : nameof(Detalle),
                new { ordenTrabajoId = resultado.Data!.OrdenTrabajoId });
        }

        private bool UsuarioId(out int usuarioId) =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);

        private static bool PuedeDecidir(PresupuestoVersion v) =>
            v.Decision == EstadoPresupuestoVersion.Pendiente &&
            v.Presupuesto.Estado == EstadoPresupuesto.Pendiente &&
            v.Presupuesto.OrdenTrabajo.EstadoActual == EstadoOrden.EsperandoAprobacion &&
            !v.Presupuesto.OrdenTrabajo.FechaFin.HasValue && v.Presupuesto.OrdenTrabajo.Factura == null;

        private static object Snapshot(PresupuestoVersion v) => new
        {
            id = v.Id, numeroVersion = v.NumeroVersion, total = v.Total,
            decision = v.Decision.ToString(), fechaEnvio = v.FechaEnvio,
            enviadaPorUsuarioId = v.EnviadaPorUsuarioId, fechaDecision = v.FechaDecision,
            decididaPorUsuarioId = v.DecididaPorUsuarioId, motivoRechazo = v.MotivoRechazo,
            items = v.Items.Select(i => new { descripcion = i.Descripcion, cantidad = i.Cantidad,
                precioUnitario = i.PrecioUnitario, subtotal = i.Subtotal, repuestoId = i.RepuestoId }),
            evidencias = v.Evidencias.Select(e => new { id = e.EvidenciaTrabajoId,
                descripcion = e.EvidenciaTrabajo.Descripcion, fecha = e.EvidenciaTrabajo.Fecha })
        };
    }
}
