using System.Security.Claims;
using MecaniCar360.Attributes;
using MecaniCar360.Models;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers;

[Authorize, Permiso("NOTIFICACION_VER")]
public sealed class NotificacionController(NotificacionService notificaciones) : Controller
{
    private int Actor => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var r = await notificaciones.ObtenerPorPersonaAsync(Actor);
        return r.Exitoso ? View(r.Data) : Forbid();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLeida(int id)
    {
        var r = await notificaciones.MarcarComoLeidaAsync(id, Actor);
        return r.Exitoso ? RedirectToAction(nameof(Index)) : NotFound();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarTodas()
    {
        var r = await notificaciones.MarcarTodasComoLeidasAsync(Actor);
        return r.Exitoso ? RedirectToAction(nameof(Index)) : Forbid();
    }

    [HttpGet]
    public async Task<IActionResult> Recurso(int id)
    {
        var r = await notificaciones.ObtenerPropiaAsync(id, Actor);
        if (!r.Exitoso) return NotFound();
        var n = r.Data!;
        if (n.RecursoId is not > 0) return NotFound();
        // Sólo IDs de la notificación propia, nunca destinos recibidos del navegador.
        // Cada destino conserva su autorización y ownership/asignación.
        return n.TipoRecurso switch
        {
            TipoRecursoNotificacion.PresupuestoPropio => RedirectToAction("Propio", "Presupuesto", new { ordenTrabajoId = n.RecursoId }),
            TipoRecursoNotificacion.OrdenPropia =>
                RedirectToAction("Orden", "PortalCliente", new { id = n.RecursoId }),
            TipoRecursoNotificacion.FacturaPropia => RedirectToAction("DetallePropio", "Factura", new { facturaId = n.RecursoId }),
            TipoRecursoNotificacion.GarantiaPropia => RedirectToAction("DetallePropio", "Garantia", new { id = n.RecursoId }),
            TipoRecursoNotificacion.OrdenTrabajo => RedirectToAction("Detalle", "OrdenTrabajo", new { id = n.RecursoId }),
            TipoRecursoNotificacion.Turno => RedirectToAction("Detalle", "Turno", new { id = n.RecursoId }),
            TipoRecursoNotificacion.Repuesto => RedirectToAction("Detalle", "Stock", new { id = n.RecursoId }),
            _ => NotFound()
        };
    }
}
