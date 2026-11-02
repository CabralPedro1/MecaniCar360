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
            if (!resultado.Exitoso) return ErrorOperacion(resultado.Mensaje);
            var repuestos = await _stock.ObtenerRepuestosTecnicosAsync(usuario);
            if (!repuestos.Exitoso)
                ViewData["ErrorRepuestos"] = "No se pudieron cargar los repuestos. Intente nuevamente antes de agregar un ítem con repuesto.";
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
            if (!resultado.Exitoso) return ErrorOperacion(resultado.Mensaje, true);
            ViewData["PuedeDecidir"] = PuedeDecidir(resultado.Data!,
                await _service.PuedeDecidirAsync(resultado.Data!.Id, usuario));
            return View(resultado.Data);
        }

        [HttpGet, Permiso("CLIENTE_PRESUPUESTO_VER")]
        public async Task<IActionResult> ObtenerPropio(int ordenTrabajoId)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            var resultado = await _service.ObtenerPropioAsync(ordenTrabajoId, usuario);
            return resultado.Exitoso
                ? Json(new { exitoso = true, version = Snapshot(resultado.Data!), puedeDecidir = PuedeDecidir(resultado.Data!,
                    await _service.PuedeDecidirAsync(resultado.Data!.Id, usuario)) })
                : Json(new { exitoso = false, mensaje = resultado.Mensaje });
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("PRESUPUESTO_CREAR")]
        public async Task<IActionResult> Crear(int ordenTrabajoId)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            if (!ModelState.IsValid) return ErrorOperacion("Datos inválidos.");
            return Resultado(await _service.CrearAsync(ordenTrabajoId, usuario));
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("PRESUPUESTO_MODIFICAR")]
        public async Task<IActionResult> AgregarItem(int presupuestoId, string descripcion,
            int cantidad, decimal precioUnitario, int? repuestoId, int ordenTrabajoId = 0)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            ViewData["OperacionRechazada"] = nameof(AgregarItem);
            if (!ModelState.IsValid) return await MostrarRechazo(ordenTrabajoId, presupuestoId, false);
            var resultado = await _service.AgregarItemAsync(presupuestoId, usuario, descripcion, cantidad, precioUnitario, repuestoId);
            if (!resultado.Exitoso) return await MostrarRechazo(ordenTrabajoId, presupuestoId, false, resultado.Mensaje);
            return Resultado(resultado);
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("PRESUPUESTO_MODIFICAR")]
        public async Task<IActionResult> EliminarItem(int presupuestoId, int itemId, int ordenTrabajoId = 0)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            if (!ModelState.IsValid) return await MostrarRechazo(ordenTrabajoId, presupuestoId, false);
            var resultado = await _service.EliminarItemAsync(presupuestoId, itemId, usuario);
            if (!resultado.Exitoso) return await MostrarRechazo(ordenTrabajoId, presupuestoId, false, resultado.Mensaje);
            return Resultado(resultado);
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("PRESUPUESTO_ENVIAR")]
        public async Task<IActionResult> EnviarAprobacion(int presupuestoId, IEnumerable<int>? evidenciaIds = null, int ordenTrabajoId = 0)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            ViewData["EvidenciasSeleccionadas"] = evidenciaIds?.ToHashSet() ?? new HashSet<int>();
            if (!ModelState.IsValid) return await MostrarRechazo(ordenTrabajoId, presupuestoId, false);
            var resultado = await _service.EnviarAprobacionAsync(presupuestoId, usuario, evidenciaIds);
            if (!resultado.Exitoso) return await MostrarRechazo(ordenTrabajoId, presupuestoId, false, resultado.Mensaje);
            return Resultado(resultado);
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("CLIENTE_PRESUPUESTO_APROBAR")]
        public async Task<IActionResult> Aprobar(int presupuestoVersionId, int ordenTrabajoId = 0)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            if (!ModelState.IsValid) return await MostrarRechazo(ordenTrabajoId, presupuestoVersionId, true);
            var resultado = await _service.AprobarAsync(presupuestoVersionId, usuario);
            if (!resultado.Exitoso) return await MostrarRechazo(ordenTrabajoId, presupuestoVersionId, true, resultado.Mensaje);
            return Resultado(resultado, true);
        }

        [HttpPost, ValidateAntiForgeryToken, Permiso("CLIENTE_PRESUPUESTO_RECHAZAR")]
        public async Task<IActionResult> Rechazar(int presupuestoVersionId, string motivo, int ordenTrabajoId = 0)
        {
            if (!UsuarioId(out var usuario)) return Forbid();
            if (!ModelState.IsValid) return await MostrarRechazo(ordenTrabajoId, presupuestoVersionId, true);
            var resultado = await _service.RechazarAsync(presupuestoVersionId, usuario, motivo);
            if (!resultado.Exitoso) return await MostrarRechazo(ordenTrabajoId, presupuestoVersionId, true, resultado.Mensaje);
            return Resultado(resultado, true);
        }

        private async Task<IActionResult> MostrarRechazo(int ordenTrabajoId, int recursoId, bool cliente, string? mensaje = null)
        {
            if (mensaje != null) ModelState.AddModelError(string.Empty, mensaje);
            // El contexto de presentación no autoriza ni cambia el recurso del POST.
            if (ordenTrabajoId <= 0) return ErrorOperacion(mensaje ?? "Revise los datos ingresados.", cliente);
            var resultado = cliente ? await Propio(ordenTrabajoId) : await Detalle(ordenTrabajoId);
            return PresentarRechazo(resultado, recursoId, cliente);
        }

        private IActionResult PresentarRechazo(IActionResult resultado, int recursoId, bool cliente)
        {
            if (resultado is not ViewResult vista) return resultado;
            if (vista.Model is not Presupuesto && vista.Model is not PresupuestoVersion) return vista;
            var coincide = cliente ? vista.Model is PresupuestoVersion v && v.Id == recursoId
                : vista.Model is Presupuesto p && p.Id == recursoId;
            if (!coincide) return ErrorOperacion("El presupuesto o su versión cambió. Revise la versión vigente antes de volver a decidir o editar.", cliente);
            if (!cliente && ViewData["OperacionRechazada"] as string == nameof(AgregarItem) &&
                ModelState["repuestoId"]?.AttemptedValue is string seleccionado && !string.IsNullOrEmpty(seleccionado) &&
                ViewData["Repuestos"] is IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> opciones &&
                !opciones.Any(o => o.Value == seleccionado))
                ModelState.AddModelError("repuestoId", "El repuesto seleccionado no está disponible en las opciones actuales. Revise la selección antes de reenviar.");
            vista.ViewName = cliente ? "Propio" : "Detalle";
            return vista;
        }

        private IActionResult Resultado(ServiceResult<PresupuestoOperacion> resultado, bool cliente = false)
        {
            if (!resultado.Exitoso) return ErrorOperacion(resultado.Mensaje, cliente);
            TempData["Ok"] = resultado.Mensaje;
            return RedirectToAction(cliente ? nameof(Propio) : nameof(Detalle),
                new { ordenTrabajoId = resultado.Data!.OrdenTrabajoId });
        }

        private ViewResult ErrorOperacion(string mensaje, bool cliente = false)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            ViewData["EsCliente"] = cliente;
            return View("ErrorOperacion", mensaje);
        }

        private bool UsuarioId(out int usuarioId) =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);

        private static bool PuedeDecidir(PresupuestoVersion v, bool actorAutorizado) =>
            actorAutorizado && v.Decision == EstadoPresupuestoVersion.Pendiente &&
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
