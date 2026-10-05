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
    {
        var existente = await calificaciones.ObtenerPorOrdenTrabajoAsync(ordenTrabajoId, Actor);
        ViewBag.Calificacion = existente.Data;
        if (existente.Exitoso)
        {
            ViewBag.PuedeCalificar = false;
            return View(new CalificacionViewModel { OrdenTrabajoId = ordenTrabajoId });
        }
        var orden = await ordenes.ObtenerPropiaAsync(ordenTrabajoId, Actor);
        if (!orden.Exitoso) return NotFound();
        ViewBag.PuedeCalificar = orden.Data!.EstadoActual == EstadoOrden.Entregado &&
            await permisos.TienePermisoAsync(Actor, "CLIENTE_CALIFICACION_CREAR");
        return View(new CalificacionViewModel { OrdenTrabajoId = ordenTrabajoId });
    }

    [HttpPost, ValidateAntiForgeryToken, Permiso("CLIENTE_CALIFICACION_CREAR")]
    public async Task<IActionResult> Crear(CalificacionViewModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var r = await calificaciones.CrearAsync(model.OrdenTrabajoId, Actor, model.Puntuacion, model.Comentario);
        TempData[r.Exitoso ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToAction(nameof(Detalle), new { ordenTrabajoId = model.OrdenTrabajoId });
    }
}
