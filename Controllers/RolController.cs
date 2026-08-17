using MecaniCar360.Models;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class RolController : Controller
    {
        private readonly RolService _service;

        public RolController(RolService service)
        {
            _service = service;
        }

        // =============================
        // INDEX
        // =============================
        public async Task<IActionResult> Index()
        {
            var resultado = await _service.ObtenerTodosAsync();

            return View(resultado.Data);
        }

        // =============================
        // DETALLE
        // =============================
        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _service.ObtenerPorIdAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        // =============================
        // CREAR
        // =============================
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Rol rol)
        {
            if (!ModelState.IsValid)
                return View(rol);

            var resultado = await _service.CrearAsync(rol);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                return View(rol);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        // =============================
        // EDITAR
        // =============================
        public async Task<IActionResult> Editar(int id)
        {
            var resultado = await _service.ObtenerPorIdAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Rol rol)
        {
            if (!ModelState.IsValid)
                return View(rol);

            var resultado = await _service.EditarAsync(rol);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                return View(rol);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // CAMBIAR ESTADO
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