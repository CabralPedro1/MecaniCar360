using MecaniCar360.Models;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers
{
    [Authorize(Roles = "ADMIN,MECANICO")]
    public class MarcaController : Controller
    {
        private readonly MarcaService _service;

        public MarcaController(MarcaService service)
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
            var resultado = await _service.ObtenerTodosAsync();

            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;
                return View(new List<Marca>());
            }

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
        public async Task<IActionResult> Crear(Marca marca)
        {
            if (!ModelState.IsValid)
                return View(marca);

            var resultado = await _service.CrearAsync(marca);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                return View(marca);
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
        public async Task<IActionResult> Editar(Marca marca)
        {
            if (!ModelState.IsValid)
                return View(marca);

            var resultado = await _service.EditarAsync(marca);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                return View(marca);
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