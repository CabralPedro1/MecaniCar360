using System.Security.Claims;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers;

[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RegistroCompletoClienteController(RegistroCompletoClienteService registro, ClienteHabilitadoService habilitado,
    SessionManager sesion) : Controller
{
    private int Actor => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var u = await registro.ObtenerAsync(Actor);
        if (u == null) return Forbid();
        if (await habilitado.EstaHabilitadoAsync(u.PersonaId)) return RedirectToAction("Index", "PortalCliente");
        return View(new RegistroCompletoClienteViewModel { Nombre = u.Persona.Nombre ?? "", Apellido = u.Persona.Apellido ?? "",
            Dni = u.Persona.Dni ?? "", Telefono = u.Persona.Telefono ?? "", Email = u.EmailLogin });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RegistroCompletoClienteViewModel vm)
    {
        var u = await registro.ObtenerAsync(Actor);
        if (u == null) return Forbid();
        if (ModelState.IsValid) {
            var r = await registro.CompletarAsync(Actor, vm);
            if (r.Exitoso) {
                // El nuevo stamp revoca las cookies anteriores; nuevo login con un método ya asociado.
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                sesion.CerrarSesion();
                TempData["Ok"] = r.Mensaje;
                return RedirectToAction("Login", "Account");
            }
            ModelState.AddModelError("", r.Mensaje);
        }
        vm.Email = u.EmailLogin;
        return View(vm);
    }
}
