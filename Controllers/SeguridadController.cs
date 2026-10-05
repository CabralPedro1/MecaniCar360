using System.Security.Claims;
using MecaniCar360.Attributes;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers;

[Authorize, Permiso(SeguridadService.Capacidad)]
public sealed class SeguridadController(SeguridadService service) : Controller
{
    private int Actor => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    [HttpGet] public async Task<IActionResult> Index()
    {
        var model = await service.ConsultarAsync(Actor);
        return model == null ? Forbid() : View(model);
    }
    [HttpGet] public async Task<IActionResult> Familia(int id)
    {
        var model = await service.ConsultarAsync(Actor);
        if (model == null) return Forbid();
        if (!model.Familias.Any(f => f.Id == id)) return NotFound();
        ViewBag.FamiliaId = id;
        return View(model);
    }
    [HttpGet] public async Task<IActionResult> Patentes()
    {
        var model = await service.ConsultarAsync(Actor);
        return model == null ? Forbid() : View(model);
    }
    [HttpGet] public async Task<IActionResult> Rol(int id)
    {
        var model = await service.ConsultarAsync(Actor);
        if (model == null) return Forbid();
        if (!model.Roles.Any(r => r.Id == id)) return NotFound();
        ViewBag.RolId = id;
        return View(model);
    }
    [HttpGet] public async Task<IActionResult> Editar(int id = 0)
    {
        var model = await service.ConsultarAsync(Actor);
        if (model == null) return Forbid();
        if (id == 0) return View(new FamiliaEdicionViewModel());
        var f = model.Familias.SingleOrDefault(f => f.Id == id);
        return f == null ? NotFound() : View(new FamiliaEdicionViewModel { Id = f.Id, Nombre = f.Nombre, Descripcion = f.Descripcion, Activo = f.Activo });
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Editar(FamiliaEdicionViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var r = await service.GuardarFamiliaAsync(model, Actor);
        if (!r.Exitoso) { ModelState.AddModelError("", r.Mensaje); return View(model); }
        TempData["Ok"] = r.Mensaje;
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Patente(int familiaId, int patenteId, bool agregar)
    {
        if (!ModelState.IsValid) return BadRequest();
        var r = await service.PatenteAsync(familiaId, patenteId, agregar, Actor);
        TempData[r.Exitoso ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToAction(nameof(Familia), new { id = familiaId });
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Hija(int familiaId, int hijaId, bool agregar)
    {
        if (!ModelState.IsValid) return BadRequest();
        var r = await service.HijaAsync(familiaId, hijaId, agregar, Actor);
        TempData[r.Exitoso ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToAction(nameof(Familia), new { id = familiaId });
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> FamiliaRol(int rolId, int familiaId, bool agregar)
    {
        if (!ModelState.IsValid) return BadRequest();
        var r = await service.RolAsync(rolId, familiaId, agregar, Actor);
        TempData[r.Exitoso ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToAction(nameof(Rol), new { id = rolId });
    }
}
