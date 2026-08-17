using MecaniCar360.Models;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MecaniCar360.Controllers
{
    [Authorize(Roles = "ADMIN,MECANICO")]
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

        public async Task<IActionResult> Index()
        {
            var resultado = await _service.ObtenerRepuestosAsync();

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

        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _service.ObtenerRepuestoAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        // ======================================
        // ADMINISTRAR PROVEEDORES
        // ======================================

        public async Task<IActionResult> AdministrarProveedores(int id)
        {
            var resultadoRepuesto = await _service.ObtenerRepuestoAsync(id);

            if (!resultadoRepuesto.Exitoso)
                return NotFound();

            var resultadoProveedores = await _service.ObtenerProveedoresDeRepuestoAsync(id);

            if (!resultadoProveedores.Exitoso)
            {
                TempData["Error"] = resultadoProveedores.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            var resultadoDisponibles = await _proveedorService.ObtenerTodosAsync();

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
        public async Task<IActionResult> AgregarProveedor(
            AdministrarProveedoresViewModel model)
        {
            var resultado = await _service.AgregarProveedorARepuestoAsync(
                model.RepuestoId,
                model.ProveedorId,
                model.PrecioCompra,
                model.CodigoProveedor,
                model.Principal);

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
        public async Task<IActionResult> CambiarProveedorPrincipal(int proveedorRepuestoId)
        {
            var relacionResultado = await _service.ObtenerRelacionProveedorAsync(proveedorRepuestoId);

            if (!relacionResultado.Exitoso)
                return NotFound();

            var resultado = await _service.CambiarProveedorPrincipalAsync(proveedorRepuestoId);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(
                nameof(AdministrarProveedores),
                new { id = relacionResultado.Data!.RepuestoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarPrecio(
            int proveedorRepuestoId,
            decimal precioCompra)
        {
            var relacionResultado = await _service.ObtenerRelacionProveedorAsync(proveedorRepuestoId);

            if (!relacionResultado.Exitoso)
                return NotFound();

            var resultado = await _service.ActualizarPrecioCompraAsync(
                proveedorRepuestoId,
                precioCompra);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(
                nameof(AdministrarProveedores),
                new { id = relacionResultado.Data!.RepuestoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarProveedor(int proveedorRepuestoId)
        {
            var relacionResultado = await _service.ObtenerRelacionProveedorAsync(proveedorRepuestoId);

            if (!relacionResultado.Exitoso)
                return NotFound();

            var resultado = await _service.EliminarProveedorDelRepuestoAsync(proveedorRepuestoId);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(
                nameof(AdministrarProveedores),
                new { id = relacionResultado.Data!.RepuestoId });
        }

        // ======================================
        // CREAR
        // ======================================

        public async Task<IActionResult> Crear()
        {
            await CargarProveedores();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Repuesto repuesto)
        {
            if (!ModelState.IsValid)
            {
                await CargarProveedores();
                return View(repuesto);
            }

            var resultado = await _service.CrearRepuestoAsync(repuesto);

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

        public async Task<IActionResult> Editar(int id)
        {
            var resultado = await _service.ObtenerRepuestoAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            await CargarProveedores();

            return View(resultado.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Repuesto repuesto)
        {
            if (!ModelState.IsValid)
            {
                await CargarProveedores();
                return View(repuesto);
            }

            var resultado = await _service.EditarRepuestoAsync(repuesto);

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
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var resultado = await _service.CambiarEstadoAsync(id);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        // ======================================
        // INGRESO
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarIngreso(
            int repuestoId,
            int proveedorRepuestoId,
            int cantidad,
            string? observaciones)
        {
            int usuarioId = int.Parse(User.FindFirst("UsuarioId")!.Value);

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
        public async Task<IActionResult> RegistrarSalida(
            int repuestoId,
            int cantidad,
            string? observaciones)
        {
            int usuarioId = int.Parse(User.FindFirst("UsuarioId")!.Value);

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
        public async Task<IActionResult> RegistrarAjuste(
            int repuestoId,
            int diferencia,
            string observaciones)
        {
            int usuarioId = int.Parse(User.FindFirst("UsuarioId")!.Value);

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

        public async Task<IActionResult> Movimientos()
        {
            var resultado = await _service.ObtenerMovimientosAsync();

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
            var resultado = await _proveedorService.ObtenerTodosAsync();

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
    }
}