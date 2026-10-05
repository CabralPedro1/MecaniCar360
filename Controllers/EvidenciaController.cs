using System.Security.Claims;
using MecaniCar360.Attributes;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MecaniCar360.Controllers;

[Authorize]
public sealed class EvidenciaController(EvidenciaTrabajoService evidencias) : Controller
{
    private int Actor => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:0;
    [HttpGet, Permiso("EVIDENCIA_CREAR")]
    public async Task<IActionResult> Crear(int ordenTrabajoId)
    {
        var r=await evidencias.ObtenerPorOrdenTrabajoAsync(ordenTrabajoId,Actor);
        return r.Exitoso && r.Data!.PuedeCargar ? View(new CargarEvidenciaViewModel{OrdenTrabajoId=ordenTrabajoId}) : Forbid();
    }
    [HttpPost, ValidateAntiForgeryToken, Permiso("EVIDENCIA_CREAR")]
    [RequestSizeLimit(AlmacenEvidencias.LimiteBytes + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = AlmacenEvidencias.LimiteBytes + 65536)]
    public async Task<IActionResult> Crear(CargarEvidenciaViewModel model)
    {
        if(!ModelState.IsValid) return View(model);
        var r=await evidencias.CrearAsync(model.OrdenTrabajoId,Actor,model.Descripcion,model.Archivo);
        if(!r.Exitoso) { ModelState.AddModelError("",r.Mensaje); return View(model); }
        return RedirectToAction("Detalle","OrdenTrabajo",new{id=model.OrdenTrabajoId});
    }
    [HttpGet, Permiso("EVIDENCIA_VER")]
    public async Task<IActionResult> Archivo(int id)
    {
        var r=await evidencias.ArchivoAsync(id,Actor);
        if(!r.Exitoso) return NotFound();
        Response.Headers["X-Content-Type-Options"]="nosniff";
        Response.Headers.CacheControl="no-store";
        return File(r.Data!.Contenido,r.Data.Mime,r.Data.Nombre);
    }
}
