using MecaniCar360.Attributes;
using System.Security.Claims;
using MecaniCar360.Models;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers
{
    [Authorize]
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

        [Permiso("VEHICULO_VER")]
        public async Task<IActionResult> Index()
        {
            var resultado = await _service.ObtenerTodosAsync(SolicitanteId());

            return View(resultado.Data);
        }

        // =====================================
        // DETALLE
        // =====================================

        [Permiso("VEHICULO_VER")]
        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _service.ObtenerTodosAsync(SolicitanteId());

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

        [Permiso("VEHICULO_MODIFICAR")]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("VEHICULO_MODIFICAR")]
        public async Task<IActionResult> Crear([Bind("Id,Nombre")] Marca marca)
        {
            if (!ModelState.IsValid)
                return View(marca);

            var resultado = await _service.CrearAsync(marca, SolicitanteId());

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

        [Permiso("VEHICULO_MODIFICAR")]
        public async Task<IActionResult> Editar(int id)
        {
            var resultado = await _service.ObtenerPorIdAsync(id, SolicitanteId());

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("VEHICULO_MODIFICAR")]
        public async Task<IActionResult> Editar([Bind("Id,Nombre")] Marca marca)
        {
            if (!ModelState.IsValid)
                return View(marca);

            var resultado = await _service.EditarAsync(marca, SolicitanteId());

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
        [Permiso("VEHICULO_MODIFICAR")]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var resultado = await _service.CambiarEstadoAsync(id, SolicitanteId());

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }
        private int SolicitanteId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    }
}