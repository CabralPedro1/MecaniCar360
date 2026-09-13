using MecaniCar360.Attributes;
using System.Security.Claims;
using MecaniCar360.Models;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class StockController : Controller
    {
        private readonly StockService _service;
        private readonly ProveedorService _proveedorService;

        public StockController(
            StockService service,
            ProveedorService proveedorService)
        {
            _service = service;
            _proveedorService = proveedorService;
        }

        // ======================================
        // LISTADO
        // ======================================

        [Permiso("STOCK_VER")]
        public async Task<IActionResult> Index()
        {
            var resultado = await _service.ObtenerRepuestosAsync(SolicitanteId());

            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;
                return View(new List<Repuesto>());
            }

            return View(resultado.Data);
        }

        // ======================================
        // DETALLE
        // ======================================

        [Permiso("STOCK_VER")]
        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _service.ObtenerRepuestoAsync(id, SolicitanteId());

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        // ======================================
        // ADMINISTRAR PROVEEDORES
        // ======================================

        [Permiso("STOCK_MODIFICAR")]
        public async Task<IActionResult> AdministrarProveedores(int id)
        {
            var resultadoRepuesto = await _service.ObtenerRepuestoAsync(id, SolicitanteId());

            if (!resultadoRepuesto.Exitoso)
                return NotFound();

            var resultadoProveedores = await _service.ObtenerProveedoresDeRepuestoAsync(id, SolicitanteId());

            if (!resultadoProveedores.Exitoso)
            {
                TempData["Error"] = resultadoProveedores.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            var resultadoDisponibles = await _proveedorService.ObtenerTodosAsync(SolicitanteId());

            if (!resultadoDisponibles.Exitoso)
            {
                TempData["Error"] = resultadoDisponibles.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            var repuesto = resultadoRepuesto.Data!;
            var proveedores = resultadoProveedores.Data!;
            var disponibles = resultadoDisponibles.Data!;

            var viewModel = new AdministrarProveedoresViewModel
            {
                RepuestoId = repuesto.Id,
                Repuesto = repuesto,
                Proveedores = proveedores,

                ProveedoresDisponibles = disponibles
                    .Where(p => !proveedores.Any(pr => pr.ProveedorId == p.Id))
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = $"{p.Nombre} {p.Apellido}"
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("STOCK_MODIFICAR")]
        public async Task<IActionResult> AgregarProveedor(
            AdministrarProveedoresViewModel model)
        {
            var resultado = await _service.AgregarProveedorARepuestoAsync(
                model.RepuestoId,
                model.ProveedorId,
                model.PrecioCompra,
                model.CodigoProveedor,
                model.Principal, SolicitanteId());

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(
                nameof(AdministrarProveedores),
                new { id = model.RepuestoId });
        }

        // ======================================
        // CAMBIAR PROVEEDOR PRINCIPAL
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("STOCK_MODIFICAR")]
        public async Task<IActionResult> CambiarProveedorPrincipal(int proveedorRepuestoId)
        {
            var relacionResultado = await _service.ObtenerRelacionProveedorAsync(proveedorRepuestoId, SolicitanteId());

            if (!relacionResultado.Exitoso)
                return NotFound();

            var resultado = await _service.CambiarProveedorPrincipalAsync(proveedorRepuestoId, SolicitanteId());

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(
                nameof(AdministrarProveedores),
                new { id = relacionResultado.Data!.RepuestoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("STOCK_MODIFICAR")]
        public async Task<IActionResult> ActualizarPrecio(
            int proveedorRepuestoId,
            decimal precioCompra)
        {
            var relacionResultado = await _service.ObtenerRelacionProveedorAsync(proveedorRepuestoId, SolicitanteId());

            if (!relacionResultado.Exitoso)
                return NotFound();

            var resultado = await _service.ActualizarPrecioCompraAsync(
                proveedorRepuestoId,
                precioCompra, SolicitanteId());

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(
                nameof(AdministrarProveedores),
                new { id = relacionResultado.Data!.RepuestoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("STOCK_MODIFICAR")]
        public async Task<IActionResult> EliminarProveedor(int proveedorRepuestoId)
        {
            var relacionResultado = await _service.ObtenerRelacionProveedorAsync(proveedorRepuestoId, SolicitanteId());

            if (!relacionResultado.Exitoso)
                return NotFound();

            var resultado = await _service.EliminarProveedorDelRepuestoAsync(proveedorRepuestoId, SolicitanteId());

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(
                nameof(AdministrarProveedores),
                new { id = relacionResultado.Data!.RepuestoId });
        }

        // ======================================
        // CREAR
        // ======================================

        [Permiso("STOCK_CREAR")]
        public async Task<IActionResult> Crear()
        {
            await CargarProveedores();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("STOCK_CREAR")]
        public async Task<IActionResult> Crear([Bind("Id,SKU,Nombre,Marca,Modelo,Compatibilidad,PrecioVenta,StockMinimo,StockActual")] Repuesto repuesto)
        {
            if (!ModelState.IsValid)
            {
                await CargarProveedores();
                return View(repuesto);
            }

            var resultado = await _service.CrearRepuestoAsync(repuesto, SolicitanteId());

            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;

                await CargarProveedores();

                return View(repuesto);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        // ======================================
        // EDITAR
        // ======================================

        [Permiso("STOCK_MODIFICAR")]
        public async Task<IActionResult> Editar(int id)
        {
            var resultado = await _service.ObtenerRepuestoAsync(id, SolicitanteId());

            if (!resultado.Exitoso)
                return NotFound();

            await CargarProveedores();

            return View(resultado.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("STOCK_MODIFICAR")]
        public async Task<IActionResult> Editar([Bind("Id,SKU,Nombre,Marca,Modelo,Compatibilidad,PrecioVenta,StockMinimo")] Repuesto repuesto)
        {
            if (!ModelState.IsValid)
            {
                await CargarProveedores();
                return View(repuesto);
            }

            var resultado = await _service.EditarRepuestoAsync(repuesto, SolicitanteId());

            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;

                await CargarProveedores();

                return View(repuesto);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        // ======================================
        // CAMBIAR ESTADO
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("STOCK_MODIFICAR")]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var resultado = await _service.CambiarEstadoAsync(id, SolicitanteId());

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        // ======================================
        // INGRESO
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("STOCK_MOVIMIENTO")]
        public async Task<IActionResult> RegistrarIngreso(
            int repuestoId,
            int proveedorRepuestoId,
            int cantidad,
            string? observaciones)
        {
            int usuarioId = SolicitanteId();

            var resultado = await _service.RegistrarIngresoAsync(
                repuestoId,
                proveedorRepuestoId,
                cantidad,
                usuarioId,
                observaciones);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Detalle), new { id = repuestoId });
        }

        // ======================================
        // SALIDA
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("STOCK_MOVIMIENTO")]
        public async Task<IActionResult> RegistrarSalida(
            int repuestoId,
            int cantidad,
            string? observaciones)
        {
            int usuarioId = SolicitanteId();

            var resultado = await _service.RegistrarSalidaAsync(
                repuestoId,
                cantidad,
                usuarioId,
                observaciones);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Detalle), new { id = repuestoId });
        }

        // ======================================
        // AJUSTE
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("STOCK_MOVIMIENTO")]
        public async Task<IActionResult> RegistrarAjuste(
            int repuestoId,
            int diferencia,
            string observaciones)
        {
            int usuarioId = SolicitanteId();

            var resultado = await _service.RegistrarAjusteAsync(
                repuestoId,
                diferencia,
                usuarioId,
                observaciones);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Detalle), new { id = repuestoId });
        }

        // ======================================
        // HISTORIAL
        // ======================================

        [Permiso("STOCK_MOVIMIENTO")]
        public async Task<IActionResult> Movimientos()
        {
            var resultado = await _service.ObtenerMovimientosAsync(SolicitanteId());

            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;
                return View(new List<MovimientoStock>());
            }

            return View(resultado.Data);
        }

        // ======================================
        // MÉTODOS PRIVADOS
        // ======================================

        private async Task CargarProveedores()
        {
            var resultado = await _proveedorService.ObtenerTodosAsync(SolicitanteId());

            if (!resultado.Exitoso)
            {
                ViewBag.Proveedores = new SelectList(
                    Enumerable.Empty<SelectListItem>());

                return;
            }

            ViewBag.Proveedores = new SelectList(
                resultado.Data!,
                "Id",
                "Nombre");
        }
        private int SolicitanteId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    }
}
