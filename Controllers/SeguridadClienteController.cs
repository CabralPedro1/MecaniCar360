using System.Security.Claims;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;

namespace MecaniCar360.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting(Program.LoginRateLimitPolicy)]
public sealed class SeguridadClienteController(SeguridadClienteService seguridad, SessionManager sesion) : Controller
{
    private int Actor => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
        base.OnActionExecuting(context);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SolicitarEnlace()
    {
        if (!await seguridad.PuedeGestionarAsync(Actor)) return Forbid();
        await seguridad.SolicitarPasswordAsync(Actor);
        return View("Solicitada");
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Recuperar() => View(new RecuperarPasswordClienteViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Recuperar(RecuperarPasswordClienteViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        await seguridad.SolicitarRecuperacionAsync(vm.Email);
        return View("Solicitada");
    }

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Restablecer([FromHeader(Name = "X-Password-Token")] string? token)
    {
        if (token == null) return View("Abrir");
        return await seguridad.ValidarEnlaceAsync(token)
            ? View(new RestablecerPasswordClienteViewModel { Token = token }) : View("NoDisponible");
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Restablecer(RestablecerPasswordClienteViewModel vm)
    {
        if (!await seguridad.ValidarEnlaceAsync(vm.Token)) return View("NoDisponible");
        if (ModelState.IsValid)
        {
            var resultado = await seguridad.RestablecerAsync(vm);
            if (resultado.Exitoso)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                sesion.CerrarSesion();
                return RedirectToAction(nameof(Completada));
            }
            ModelState.AddModelError("", resultado.Mensaje);
        }
        foreach (var campo in new[] { "Password", "ConfirmarPassword" })
            if (ModelState.TryGetValue(campo, out var entry)) { entry.RawValue = null; entry.AttemptedValue = null; }
        vm.Password = vm.ConfirmarPassword = "";
        return View(vm);
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Completada() => View();
}
