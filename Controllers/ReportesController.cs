using System.Security.Claims;
using MecaniCar360.Attributes;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MecaniCar360.Controllers;

[Authorize, Permiso("REPORTES_VER")]
public sealed class ReportesController(ReportesService reportes, TimeProvider reloj) : Controller
{
    private int Actor => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    [HttpGet]
    public IActionResult Index() => View(new ReporteFiltro {
        Desde = new DateTime(reloj.GetLocalNow().Year, reloj.GetLocalNow().Month, 1), Hasta = reloj.GetLocalNow().Date });
    [HttpGet]
    public async Task<IActionResult> Resultado(ReporteFiltro filtro)
    {
        if (!ModelState.IsValid) return View("Index", filtro);
        var r = await reportes.ObtenerAsync(filtro, Actor);
        if (!r.Exitoso) { ModelState.AddModelError("", r.Mensaje); return View("Index", filtro); }
        return View(r.Data);
    }
    [HttpGet]
    public async Task<IActionResult> Pdf(ReporteFiltro filtro)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var r = await reportes.ObtenerAsync(filtro, Actor);
        if (!r.Exitoso) return BadRequest(r.Mensaje);
        return File(ReportePdf.Generar(r.Data!, reloj.GetLocalNow().DateTime), "application/pdf",
            $"rendimiento-{filtro.Desde:yyyyMMdd}-{filtro.Hasta:yyyyMMdd}.pdf");
    }
}
