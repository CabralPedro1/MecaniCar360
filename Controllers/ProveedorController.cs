using MecaniCar360.Attributes;
using System.Security.Claims;
using MecaniCar360.Models;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers
{
    [Authorize]
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

        [Permiso("PROVEEDOR_VER")]
        public async Task<IActionResult> Index()
        {
            var resultado = await _service.ObtenerTodosAsync(SolicitanteId());

            return View(resultado.Data);
        }

        // =====================================
        // DETALLE
        // =====================================

        [Permiso("PROVEEDOR_VER")]
        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _service.ObtenerPorIdAsync(id, SolicitanteId());

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        // =====================================
        // CREAR
        // =====================================

        [Permiso("PROVEEDOR_CREAR")]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("PROVEEDOR_CREAR")]
        public async Task<IActionResult> Crear([Bind("Id,Nombre,Apellido,Telefono,Email")] Proveedor proveedor)
        {
            if (!ModelState.IsValid)
                return View(proveedor);

            var resultado = await _service.CrearAsync(proveedor, SolicitanteId());

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

        [Permiso("PROVEEDOR_MODIFICAR")]
        public async Task<IActionResult> Editar(int id)
        {
            var resultado = await _service.ObtenerPorIdAsync(id, SolicitanteId());

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("PROVEEDOR_MODIFICAR")]
        public async Task<IActionResult> Editar([Bind("Id,Nombre,Apellido,Telefono,Email")] Proveedor proveedor)
        {
            if (!ModelState.IsValid)
                return View(proveedor);

            var resultado = await _service.EditarAsync(proveedor, SolicitanteId());

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
        [Permiso("PROVEEDOR_DESACTIVAR")]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var resultado = await _service.CambiarEstadoAsync(id, SolicitanteId());

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }
        private int SolicitanteId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    }
}