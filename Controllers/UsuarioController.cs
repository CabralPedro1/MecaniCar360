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
        private readonly AltaPersonalService _altaPersonal;

        public UsuarioController(UsuarioService usuarioService, AltaPersonalService altaPersonal)
        {
            _usuarioService = usuarioService;
            _altaPersonal = altaPersonal;
        }

        [HttpGet, Permiso("PERSONA_CREAR"), Permiso("USUARIO_CREAR"), Permiso("ROL_MODIFICAR")]
        public async Task<IActionResult> NuevoPersonal()
        {
            if (!await CargarRolesPersonalAsync()) return Forbid();
            return View(new NuevoPersonalViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Permiso("PERSONA_CREAR"), Permiso("USUARIO_CREAR"), Permiso("ROL_MODIFICAR")]
        public async Task<IActionResult> NuevoPersonal(NuevoPersonalViewModel model)
        {
            if (!await CargarRolesPersonalAsync(model.RolId)) return Forbid();
            if (!ModelState.IsValid) return View(model);
            var resultado = await _altaPersonal.CrearAsync(model, ObtenerUsuarioId());
            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                if (!await CargarRolesPersonalAsync(model.RolId)) return Forbid();
                return View(model);
            }
            TempData["Ok"] = resultado.Mensaje;
            return RedirectToAction(nameof(Administracion), new { seccion = "personal" });
        }

        private async Task<bool> CargarRolesPersonalAsync(int? seleccionado = null)
        {
            var resultado = await _altaPersonal.ObtenerRolesAsync(ObtenerUsuarioId());
            if (!resultado.Exitoso) return false;
            ViewBag.RolesPersonal = new SelectList(resultado.Data, "Id", "Nombre", seleccionado);
            return true;
        }

        [HttpGet, Permiso("PERSONA_VER"), Permiso("USUARIO_VER")]
        public async Task<IActionResult> Administracion()
        {
            var resultado = await _usuarioService.ObtenerAdministracionAsync(ObtenerUsuarioId());
            if (!resultado.Exitoso) return Forbid();
            return View(new AdministracionCuentasViewModel { Personas = resultado.Data! });
        }

        [HttpGet, Permiso("PERSONA_VER"), Permiso("USUARIO_VER")]
        public async Task<IActionResult> PersonaCuenta(int id)
        {
            var resultado = await _usuarioService.ObtenerAdministracionAsync(ObtenerUsuarioId(), id);
            if (!resultado.Exitoso) return Forbid();
            var persona = resultado.Data!.SingleOrDefault();
            return persona == null ? NotFound() : View(persona);
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
