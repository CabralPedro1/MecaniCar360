using MecaniCar360.Helpers;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace MecaniCar360.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;
        private readonly EmailService _emailService;
        private readonly SessionManager _sessionManager;
        private readonly AuditoriaService _auditoria;
        private readonly PermisoService _permisos;

        public AccountController(
            AccountService accountService,
            EmailService emailService,
            SessionManager sessionManager,
            AuditoriaService auditoria,
            PermisoService permisos)
        {
            _accountService = accountService;
            _emailService = emailService;
            _sessionManager = sessionManager;
            _auditoria = auditoria;
            _permisos = permisos;
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
        [EnableRateLimiting(Program.LoginRateLimitPolicy)]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Usuario o contraseña incorrectos.";
                return View();
            }

            var resultado = await _accountService.LoginAsync(username, password);

            if (!resultado.Exitoso)
            {
                await _auditoria.RegistrarLoginFallidoAsync();
                ViewBag.Error = resultado.Mensaje;
                return View();
            }

            var identity = _accountService.CrearIdentity(resultado);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            HttpContext.User = principal;
            _sessionManager.IniciarSesion();
            await _auditoria.RegistrarLoginExitosoAsync();

            if (resultado.Usuario!.PrimerLogin)
                return RedirectToAction(nameof(CompletarDatos));

            return await DestinoInicialAsync(resultado.Usuario.Id);
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
                return await DestinoInicialAsync(usuarioId);

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

            await RenovarSesionAsync(resultado.Data!);
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

            await RenovarSesionAsync(resultado.Data!);
            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(CompletarDatosExito));
        }

        private async Task RenovarSesionAsync(ClaimsIdentity identity)
        {
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            HttpContext.User = principal;
            _sessionManager.IniciarSesion();
        }

        [Authorize]
        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }

        private async Task<IActionResult> DestinoInicialAsync(int usuarioId)
        {
            if (await _permisos.TienePermisoAsync(usuarioId, "DASHBOARD_VER"))
                return RedirectToAction("Index", "Dashboard");
            if (await _permisos.TieneAlgunoAsync(usuarioId, "CLIENTE_VEHICULO_VER", "CLIENTE_TURNO_VER",
                "CLIENTE_ORDEN_VER", "CLIENTE_PRESUPUESTO_VER", "CLIENTE_FACTURA_VER", "GARANTIA_VER_PROPIA"))
                return RedirectToAction("Index", "PortalCliente");
            if (await _permisos.TienePermisoAsync(usuarioId, "TURNO_VER"))
                return RedirectToAction("Index", "Turno");
            if (await _permisos.TienePermisoAsync(usuarioId, "ORDEN_VER"))
                return RedirectToAction("Index", "OrdenTrabajo");
            if (await _permisos.TienePermisoAsync(usuarioId, "STOCK_VER"))
                return RedirectToAction("Index", "Stock");
            return RedirectToAction("Index", "Home");
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
            try
            {
                await _auditoria.RegistrarLogoutAsync();
            }
            finally
            {
                _sessionManager.CerrarSesion();
                await HttpContext.SignOutAsync();
            }

            return RedirectToAction(nameof(Login));
        }
    }
}
