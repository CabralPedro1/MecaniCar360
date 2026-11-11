using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;

namespace MecaniCar360.Controllers;

[AllowAnonymous, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting(Program.LoginRateLimitPolicy)]
public sealed class ActivacionAdminController(ActivacionAdminService activacion) : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
        base.OnActionExecuting(context);
    }

    [HttpGet]
    public async Task<IActionResult> Activar([FromHeader(Name = "X-Password-Token")] string? token)
    {
        if (token == null) return View("Abrir");
        return await activacion.ValidarAsync(token)
            ? View(new ActivarAdminViewModel { Token = token }) : View("NoDisponible");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(ActivarAdminViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var resultado = await activacion.ActivarAsync(vm);
            if (resultado.Exitoso) return RedirectToAction(nameof(Completada));
            ModelState.AddModelError("", resultado.Mensaje);
        }
        // Nunca devolver contraseñas en atributos HTML, ModelState o TempData.
        foreach (var campo in new[] { nameof(vm.Password), nameof(vm.ConfirmarPassword) })
            if (ModelState.TryGetValue(campo, out var entry)) { entry.RawValue = null; entry.AttemptedValue = null; }
        vm.Password = vm.ConfirmarPassword = "";
        return View(vm);
    }

    [HttpGet]
    public IActionResult Completada() => View();
}
