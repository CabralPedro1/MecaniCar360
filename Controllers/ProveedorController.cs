using MecaniCar360.Models;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers
{
    [Authorize(Roles = "ADMIN,MECANICO")]
    public class ProveedorController : Controller
    {
        private readonly ProveedorService _service;

        public ProveedorController(ProveedorService service)
        {
            _service = service;
        }

        // =====================================
        // LISTADO
        // =====================================

        public async Task<IActionResult> Index()
        {
            var resultado = await _service.ObtenerTodosAsync();

            return View(resultado.Data);
        }

        // =====================================
        // DETALLE
        // =====================================

        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _service.ObtenerPorIdAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        // =====================================
        // CREAR
        // =====================================

        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Proveedor proveedor)
        {
            if (!ModelState.IsValid)
                return View(proveedor);

            var resultado = await _service.CrearAsync(proveedor);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError("", resultado.Mensaje);
                return View(proveedor);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // EDITAR
        // =====================================

        public async Task<IActionResult> Editar(int id)
        {
            var resultado = await _service.ObtenerPorIdAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Proveedor proveedor)
        {
            if (!ModelState.IsValid)
                return View(proveedor);

            var resultado = await _service.EditarAsync(proveedor);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError("", resultado.Mensaje);
                return View(proveedor);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // ELIMINAR
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var resultado = await _service.CambiarEstadoAsync(id);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }
    }
}