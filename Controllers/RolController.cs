using System.Security.Claims;
using MecaniCar360.Attributes;
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
        [Permiso("ROL_VER")]
        public async Task<IActionResult> Index()
        {
            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado = await _service.ObtenerTodosAsync(usuarioId.Value);

            return View(resultado.Data);
        }

        // =============================
        // DETALLE
        // =============================
        [Permiso("ROL_VER")]
        public async Task<IActionResult> Detalle(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado = await _service.ObtenerPorIdAsync(
                id,
                usuarioId.Value);

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        // =============================
        // CREAR
        // =============================
        [Permiso("ROL_CREAR")]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("ROL_CREAR")]
        public async Task<IActionResult> Crear(Rol rol)
        {
            if (!ModelState.IsValid)
                return View(rol);

            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado = await _service.CrearAsync(rol, usuarioId.Value);

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
        [Permiso("ROL_MODIFICAR")]
        public async Task<IActionResult> Editar(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado = await _service.ObtenerPorIdParaEditarAsync(
                id,
                usuarioId.Value);

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("ROL_MODIFICAR")]
        public async Task<IActionResult> Editar(Rol rol)
        {
            if (!ModelState.IsValid)
                return View(rol);

            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado = await _service.EditarAsync(rol, usuarioId.Value);

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
        [Permiso("ROL_DESACTIVAR")]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado = await _service.CambiarEstadoAsync(id, usuarioId.Value);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        private int? ObtenerUsuarioActualId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null || !int.TryParse(claim.Value, out int usuarioId))
                return null;

            return usuarioId;
        }
    }
}