using MecaniCar360.Models;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MecaniCar360.Controllers
{
    [Authorize(Roles = "ADMIN,MECANICO")]
    public class ModeloController : Controller
    {
        private readonly ModeloService _service;
        private readonly MarcaService _marcaService;

        public ModeloController(
            ModeloService service,
            MarcaService marcaService)
        {
            _service = service;
            _marcaService = marcaService;
        }

        //====================================
        // LISTADO
        //====================================

        public async Task<IActionResult> Index()
        {
            var resultado = await _service.ObtenerTodosAsync();

            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;
                return View(new List<Modelo>());
            }

            return View(resultado.Data);
        }

        //====================================
        // DETALLE
        //====================================

        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _service.ObtenerPorIdAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        //====================================
        // CREAR
        //====================================

        public async Task<IActionResult> Crear()
        {
            await CargarMarcasAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Modelo modelo)
        {
            if (!ModelState.IsValid)
            {
                await CargarMarcasAsync();
                return View(modelo);
            }

            var resultado = await _service.CrearAsync(modelo);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                await CargarMarcasAsync();
                return View(modelo);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        //====================================
        // EDITAR
        //====================================

        public async Task<IActionResult> Editar(int id)
        {
            var resultado = await _service.ObtenerPorIdAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            await CargarMarcasAsync();

            return View(resultado.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Modelo modelo)
        {
            if (!ModelState.IsValid)
            {
                await CargarMarcasAsync();
                return View(modelo);
            }

            var resultado = await _service.EditarAsync(modelo);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                await CargarMarcasAsync();
                return View(modelo);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        //====================================
        // CAMBIAR ESTADO
        //====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var resultado = await _service.CambiarEstadoAsync(id);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        //====================================
        // MÉTODOS PRIVADOS
        //====================================

        private async Task CargarMarcasAsync()
        {
            var resultado = await _marcaService.ObtenerTodosAsync();

            ViewBag.Marcas = new SelectList(
                resultado.Data!
                    .Where(m => m.Activo)
                    .OrderBy(m => m.Nombre),
                "Id",
                "Nombre");
        }
    }
}