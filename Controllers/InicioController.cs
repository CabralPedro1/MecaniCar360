using System.Security.Claims;
using MecaniCar360.Attributes;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers;

[Authorize]
public sealed class InicioController(InicioOperativoService service) : Controller
{
    private async Task<IActionResult> Mostrar(string area)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Forbid();
        var model = await service.ObtenerAsync(area, actor);
        return model == null ? Forbid() : View("Operativo", model);
    }
    [HttpGet, Permiso("TURNO_VER")] public Task<IActionResult> Caja() => Mostrar("Caja");
    [HttpGet, Permiso("ORDEN_MODIFICAR")] public Task<IActionResult> Mecanico() => Mostrar("Mecanico");
    [HttpGet, Permiso("STOCK_VER")] public Task<IActionResult> Stock() => Mostrar("Stock");
}
