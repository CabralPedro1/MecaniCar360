using System.Security.Claims;
using MecaniCar360.Attributes;
using MecaniCar360.Models.Enums;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers;

[Authorize]
public sealed class CalificacionController(CalificacionTrabajoService calificaciones,
    OrdenTrabajoService ordenes, PermisoService permisos) : Controller
{
    private int Actor => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet, Permiso("CLIENTE_ORDEN_VER")]
    public async Task<IActionResult> Detalle(int ordenTrabajoId)
        => await MostrarDetalle(new CalificacionViewModel { OrdenTrabajoId = ordenTrabajoId });

    private async Task<IActionResult> MostrarDetalle(CalificacionViewModel model)
    {
        var ordenTrabajoId = model.OrdenTrabajoId;
        var existente = await calificaciones.ObtenerPorOrdenTrabajoAsync(ordenTrabajoId, Actor);
        ViewBag.Calificacion = existente.Data;
        if (existente.Exitoso)
        {
            ViewBag.PuedeCalificar = false;
            return View("Detalle", model);
        }
        var orden = await ordenes.ObtenerPropiaAsync(ordenTrabajoId, Actor);
        if (!orden.Exitoso) return NotFound();
        ViewBag.PuedeCalificar = await permisos.TienePermisoAsync(Actor, "CLIENTE_CALIFICACION_CREAR") &&
            await calificaciones.PuedeCalificarAsync(ordenTrabajoId, Actor);
        return View("Detalle", model);
    }

    [HttpPost, ValidateAntiForgeryToken, Permiso("CLIENTE_CALIFICACION_CREAR")]
    public async Task<IActionResult> Crear(CalificacionViewModel model)
    {
        if (!ModelState.IsValid) return await MostrarDetalle(model);
        var r = await calificaciones.CrearAsync(model.OrdenTrabajoId, Actor, model.Puntuacion, model.Comentario);
        if (!r.Exitoso)
        {
            ModelState.AddModelError(string.Empty, r.Mensaje);
            return await MostrarDetalle(model);
        }
        TempData["Ok"] = r.Mensaje;
        return RedirectToAction(nameof(Detalle), new { ordenTrabajoId = model.OrdenTrabajoId });
    }
}
