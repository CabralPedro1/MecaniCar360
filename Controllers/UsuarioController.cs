using System.Security.Claims;
using MecaniCar360.Attributes;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class UsuarioController : Controller
    {
        private readonly UsuarioService _usuarioService;

        public UsuarioController(UsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [Permiso("USUARIO_VER")]
        public async Task<IActionResult> Index()
        {
            var resultado = await _usuarioService.ObtenerTodosAsync(
                ObtenerUsuarioId());

            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;
                return View(new List<MecaniCar360.Models.Usuario>());
            }

            return View(resultado.Data);
        }

        [Permiso("USUARIO_VER")]
        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _usuarioService.ObtenerPorIdAsync(
                id,
                ObtenerUsuarioId());

            if (!resultado.Exitoso)
                return resultado.Mensaje == "No puede administrar esta cuenta."
                    ? Forbid()
                    : NotFound();

            return View(resultado.Data);
        }

        [Permiso("USUARIO_CREAR")]
        public async Task<IActionResult> Crear()
        {
            var model = new UsuarioCreateViewModel();
            await CargarPersonasAsync(model.PersonaId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("USUARIO_CREAR")]
        public async Task<IActionResult> Crear(UsuarioCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarPersonasAsync(model.PersonaId);
                return View(model);
            }

            var resultado = await _usuarioService.CrearAsync(
                model,
                ObtenerUsuarioId());

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                await CargarPersonasAsync(model.PersonaId);
                return View(model);
            }

            TempData["Ok"] = resultado.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        [Permiso("USUARIO_MODIFICAR")]
        public async Task<IActionResult> Editar(int id)
        {
            var resultado = await _usuarioService.ObtenerPorIdAsync(
                id,
                ObtenerUsuarioId());

            if (!resultado.Exitoso)
                return resultado.Mensaje == "No puede administrar esta cuenta."
                    ? Forbid()
                    : NotFound();

            return View(new UsuarioEditViewModel
            {
                Id = resultado.Data!.Id,
                Username = resultado.Data.Username,
                EmailLogin = resultado.Data.EmailLogin
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("USUARIO_MODIFICAR")]
        public async Task<IActionResult> Editar(UsuarioEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var resultado = await _usuarioService.EditarAsync(
                model,
                ObtenerUsuarioId());

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                return View(model);
            }

            TempData["Ok"] = resultado.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("USUARIO_DESACTIVAR")]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var resultado = await _usuarioService.CambiarEstadoAsync(
                id,
                ObtenerUsuarioId());

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarPersonasAsync(int? seleccionada)
        {
            var resultado = await _usuarioService
                .ObtenerPersonasDisponiblesAsync(ObtenerUsuarioId());

            ViewBag.Personas = new SelectList(
                resultado.Exitoso
                    ? resultado.Data
                    : new List<MecaniCar360.Models.Persona>(),
                "Id",
                "Nombre",
                seleccionada);
        }

        private int ObtenerUsuarioId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(claim, out int usuarioId))
                throw new InvalidOperationException(
                    "No se pudo identificar al usuario actual.");

            return usuarioId;
        }
    }
}
