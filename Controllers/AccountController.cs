using MecaniCar360.Helpers;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MecaniCar360.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;
        private readonly EmailService _emailService;

        public AccountController(
            AccountService accountService,
            EmailService emailService)
        {
            _accountService = accountService;
            _emailService = emailService;
        }

        // =====================================
        // LOGIN
        // =====================================

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var resultado = await _accountService.LoginAsync(username, password);

            if (!resultado.Exitoso)
            {
                ViewBag.Error = resultado.Mensaje;
                return View();
            }

            var identity = _accountService.CrearIdentity(resultado);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            if (resultado.Usuario!.PrimerLogin)
                return RedirectToAction(nameof(CompletarDatos));

            return RedirectToAction("Index", "Dashboard");
        }

        // =====================================
        // COMPLETAR DATOS
        // =====================================

        [Authorize]
        public async Task<IActionResult> CompletarDatos()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(claim, out int usuarioId))
                return RedirectToAction(nameof(Login));

            var resultado = await _accountService.ObtenerUsuarioAsync(usuarioId);

            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;
                return RedirectToAction(nameof(Login));
            }

            var usuario = resultado.Data!;

            if (!usuario.PrimerLogin)
                return RedirectToAction("Index", "Dashboard");

            var model = new CompletarDatosViewModel
            {
                Username = usuario.Username,
                Email = usuario.EmailLogin,
                Telefono = usuario.Persona.Telefono
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletarDatos(CompletarDatosViewModel model)
        {
            if (!int.TryParse(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    out int usuarioId))
                return Forbid();

            var usuarioResultado = await _accountService.ObtenerUsuarioAsync(usuarioId);

            if (!usuarioResultado.Exitoso)
            {
                TempData["Error"] = usuarioResultado.Mensaje;
                return RedirectToAction(nameof(Login));
            }

            // Los datos informativos siempre provienen del usuario autenticado.
            model.Username = usuarioResultado.Data!.Username;
            model.Email = usuarioResultado.Data.EmailLogin;

            if (!ModelState.IsValid)
                return View(model);

            var resultado = await _accountService.CompletarDatosAsync(
                model,
                usuarioId);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                return View(model);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(CompletarDatosExito));
        }

        [Authorize]
        public IActionResult CambiarContraseña()
        {
            return View(new CambiarContraseñaViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContraseña(
            CambiarContraseñaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(claim, out int usuarioId))
                return Forbid();

            var resultado = await _accountService
                .CambiarContraseñaAsync(model, usuarioId);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(
                    string.Empty,
                    resultado.Mensaje);

                return View(model);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(CompletarDatosExito));
        }

        [Authorize]
        public IActionResult CompletarDatosExito()
        {
            return View();
        }

        // =====================================
        // LOGOUT
        // =====================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();

            return RedirectToAction(nameof(Login));
        }
    }
}
