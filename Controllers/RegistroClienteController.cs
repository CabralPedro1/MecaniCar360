using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MecaniCar360.Controllers;

[AllowAnonymous, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RegistroClienteController(RegistroClienteService registro, SeguridadClienteService seguridad) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new RegistroClienteViewModel());

    [HttpGet]
    public IActionResult Orientacion() => View(new VerificarCorreoClienteViewModel());

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(Program.LoginRateLimitPolicy)]
    public async Task<IActionResult> Orientacion(VerificarCorreoClienteViewModel vm)
    {
        var mensaje = ModelState.IsValid ? await seguridad.OrientarAsync(vm.Codigo) : null;
        if (mensaje != null) ViewBag.Orientacion = mensaje;
        else ModelState.AddModelError("", "Código no válido o vencido.");
        if (ModelState.TryGetValue(nameof(vm.Codigo), out var entry)) { entry.RawValue = null; entry.AttemptedValue = null; }
        return View(new VerificarCorreoClienteViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(Program.LoginRateLimitPolicy)]
    public async Task<IActionResult> Index(RegistroClienteViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var resultado = await registro.SolicitarAsync(model);
        if (!resultado.Exitoso) { ModelState.AddModelError("", resultado.Mensaje); return View(model); }
        return View("Solicitada");
    }
}
