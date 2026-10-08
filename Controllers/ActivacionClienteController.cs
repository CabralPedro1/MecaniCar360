using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MecaniCar360.Controllers;

[AllowAnonymous, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ActivacionClienteController(InvitacionClienteService invitaciones) : Controller
{
    private void ProtegerRespuesta()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromHeader(Name = "X-Invitacion")] string? token)
    {
        ProtegerRespuesta();
        if (token == null) return View("Abrir");
        if (!await invitaciones.ValidarAsync(token)) return View("NoDisponible");
        return View(new ActivarClienteViewModel { Token = token });
    }

    [HttpPost, ValidateAntiForgeryToken, Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(Program.LoginRateLimitPolicy)]
    public async Task<IActionResult> Index(ActivarClienteViewModel model)
    {
        ProtegerRespuesta();
        if (!await invitaciones.ValidarAsync(model.Token)) return View("NoDisponible");
        if (ModelState.IsValid)
        {
            var resultado = await invitaciones.ActivarAsync(model);
            if (resultado.Exitoso) return RedirectToAction(nameof(Completada));
            ModelState.AddModelError("", resultado.Mensaje);
        }
        // Las contrasenas no vuelven a incluirse en HTML ni en TempData.
        foreach (var campo in new[] { nameof(model.Password), nameof(model.ConfirmarPassword) })
            if (ModelState.TryGetValue(campo, out var entrada))
            { entrada.RawValue = null; entrada.AttemptedValue = null; }
        model.Password = model.ConfirmarPassword = "";
        return View(model);
    }

    [HttpGet]
    public IActionResult Completada() { ProtegerRespuesta(); return View(); }
}
