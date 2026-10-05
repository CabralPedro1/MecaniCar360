using System.Security.Claims;
using MecaniCar360.Attributes;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers;

[Authorize]
public sealed class DashboardController(DashboardService dashboard) : Controller
{
    [HttpGet, Permiso("DASHBOARD_VER")]
    public async Task<IActionResult> Index()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Forbid();
        var r = await dashboard.ObtenerAsync(actor);
        return r.Exitoso ? View(r.Data) : Forbid();
    }
}
