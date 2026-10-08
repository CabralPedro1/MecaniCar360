using System.Security.Claims;
using MecaniCar360.Attributes;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MecaniCar360.Controllers;

// Adaptador MVC de las operaciones propias existentes. Los endpoints JSON se conservan.
[Authorize]
public sealed class PortalClienteController(
    VehiculoService vehiculos, TurnoService turnos, OrdenTrabajoService ordenes,
    PresupuestoService presupuestos, FacturaService facturas,
    PermisoService permisos, VehiculoPropioAltaService altaVehiculo, DatosClienteService datosCliente) : Controller
{
    private int Actor => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet, Permiso("CLIENTE_VEHICULO_VER", "CLIENTE_TURNO_VER", "CLIENTE_ORDEN_VER", "CLIENTE_PRESUPUESTO_VER", "CLIENTE_FACTURA_VER", "GARANTIA_VER_PROPIA")]
    public async Task<IActionResult> Index()
    {
        ViewBag.DatosPendientes = await datosCliente.PendientesRecepcionAsync(Actor);
        return View();
    }

    [HttpGet, Permiso("CLIENTE_VEHICULO_CREAR")]
    public async Task<IActionResult> NuevoVehiculo()
    {
        var r = await altaVehiculo.CatalogoAsync(Actor);
        if (!r.Exitoso) return Forbid();
        ViewBag.Modelos = new SelectList(r.Data, "Id", "Nombre");
        return View(new VehiculoPropioNuevoViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken, Permiso("CLIENTE_VEHICULO_CREAR")]
    public async Task<IActionResult> NuevoVehiculo(VehiculoPropioNuevoViewModel model)
    {
        var catalogo = await altaVehiculo.CatalogoAsync(Actor);
        if (!catalogo.Exitoso) return Forbid();
        if (ModelState.IsValid)
        {
            var r = await altaVehiculo.CrearAsync(model, Actor);
            if (r.Exitoso) { TempData["Ok"] = r.Mensaje; return RedirectToAction(nameof(MisVehiculos)); }
            ModelState.AddModelError("", r.Mensaje);
        }
        ViewBag.Modelos = new SelectList(catalogo.Data, "Id", "Nombre", model.ModeloId);
        return View(model);
    }

    [HttpGet, Permiso("CLIENTE_VEHICULO_VER")]
    public async Task<IActionResult> MisVehiculos()
    {
        var r = await vehiculos.ObtenerVehiculosPropiosAsync(Actor);
        return r.Exitoso ? View(r.Data) : Forbid();
    }

    [HttpGet, Permiso("CLIENTE_VEHICULO_VER")]
    public async Task<IActionResult> Vehiculo(int id)
    {
        var r = await vehiculos.ObtenerVehiculoPropioAsync(Actor, id);
        return r.Exitoso ? View(r.Data) : NotFound();
    }

    [HttpGet, Permiso("CLIENTE_TURNO_VER")]
    public async Task<IActionResult> MisTurnos()
    {
        var r = await turnos.ObtenerTurnosPropiosAsync(Actor);
        return r.Exitoso ? View(r.Data) : Forbid();
    }

    [HttpGet, Permiso("CLIENTE_TURNO_VER")]
    public async Task<IActionResult> Turno(int id)
    {
        var r = await turnos.ObtenerTurnoPropioAsync(Actor, id);
        return r.Exitoso ? View(r.Data) : NotFound();
    }

    [HttpGet, Permiso("CLIENTE_TURNO_CREAR")]
    public async Task<IActionResult> NuevoTurno(int? vehiculoId = null)
    {
        var model = new NuevoTurnoPropioViewModel { VehiculoId = vehiculoId };
        return await CargarVehiculosAsync(model) ? View(model) : Forbid();
    }

    [HttpPost, ValidateAntiForgeryToken, Permiso("CLIENTE_TURNO_CREAR")]
    public async Task<IActionResult> NuevoTurno(NuevoTurnoPropioViewModel model)
    {
        if (!await CargarVehiculosAsync(model)) return Forbid();
        if (ModelState.IsValid)
        {
            var r = await turnos.CrearPropioAsync(Actor, model.VehiculoId, model.Tipo,
                model.FechaInicio, model.Motivo, model.Observaciones);
            if (r.Exitoso)
            {
                TempData["Ok"] = r.Mensaje;
                return RedirectToAction(nameof(MisTurnos));
            }
            ModelState.AddModelError(string.Empty, r.Mensaje);
        }
        return View(model);
    }

    private async Task<bool> CargarVehiculosAsync(NuevoTurnoPropioViewModel model)
    {
        var r = await vehiculos.ObtenerVehiculosPropiosAsync(Actor);
        if (!r.Exitoso) return false;
        ViewBag.Vehiculos = new SelectList(r.Data!.Select(v => new
        { v.Id, Nombre = $"{v.Patente} — {v.Marca.Nombre} {v.Modelo.Nombre}" }), "Id", "Nombre", model.VehiculoId);
        return true;
    }

    [HttpPost, ValidateAntiForgeryToken, Permiso("CLIENTE_TURNO_CANCELAR")]
    public async Task<IActionResult> CancelarTurno(int id, string? motivo)
    {
        if (!ModelState.IsValid) return BadRequest();
        var r = await turnos.CancelarPropioAsync(id, Actor, motivo);
        TempData[r.Exitoso ? "Ok" : "Error"] = r.Mensaje;
        return RedirectToAction(nameof(Turno), new { id });
    }

    [HttpGet, Permiso("CLIENTE_ORDEN_VER")]
    public async Task<IActionResult> MisOrdenes()
    {
        var r = await ordenes.ObtenerPropiasAsync(Actor);
        return r.Exitoso ? View(r.Data) : Forbid();
    }

    [HttpGet, Permiso("CLIENTE_ORDEN_VER")]
    public async Task<IActionResult> Orden(int id)
    {
        var r = await ordenes.ObtenerPropiaAsync(id, Actor);
        if (!r.Exitoso) return NotFound();
        var presupuesto = await permisos.TienePermisoAsync(Actor, "CLIENTE_PRESUPUESTO_VER")
            ? (await presupuestos.ObtenerPropioAsync(id, Actor)).Data : null;
        var factura = await permisos.TienePermisoAsync(Actor, "CLIENTE_FACTURA_VER")
            ? (await facturas.ObtenerPorOrdenPropiaAsync(id, Actor)).Data : null;
        return View(new OrdenPropiaViewModel
        { Orden = r.Data!, Presupuesto = presupuesto, FacturaId = factura?.Id, Diagnostico = r.Data!.Diagnostico });
    }

    [HttpGet, Permiso("CLIENTE_PRESUPUESTO_VER")]
    public async Task<IActionResult> MisPresupuestos()
    {
        var r = await presupuestos.ListarPropiosAsync(Actor);
        return r.Exitoso ? View(r.Data) : Forbid();
    }
}
