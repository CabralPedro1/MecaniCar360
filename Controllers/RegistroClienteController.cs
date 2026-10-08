using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MecaniCar360.Controllers;

[AllowAnonymous, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RegistroClienteController(RegistroClienteService registro) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new RegistroClienteViewModel());

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(Program.LoginRateLimitPolicy)]
    public async Task<IActionResult> Index(RegistroClienteViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var resultado = await registro.SolicitarAsync(model);
        if (!resultado.Exitoso) { ModelState.AddModelError("", resultado.Mensaje); return View(model); }
        return View("Solicitada");
    }
}
