using MecaniCar360.Attributes;
using MecaniCar360.Data;
using MecaniCar360.Models.Enums;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly MecaniCarContext _context;
        private readonly PermisoService _permisos;
        public DashboardController(MecaniCarContext context, PermisoService permisos)
        {
            _context = context;
            _permisos = permisos;
        }

        // No existe patente de Dashboard: cada bloque exige su autorización funcional.
        [HttpGet]
        [Permiso("CLIENTE_TURNO_VER", "CLIENTE_PRESUPUESTO_VER", "ORDEN_VER", "TURNO_VER", "FACTURA_VER", "STOCK_ALERTAS", "PAGO_VER")]
        public async Task<IActionResult> Index()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)) return Forbid();
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioId);
            if (!personaId.HasValue) return Forbid();
            var model = new DashboardViewModel();
            var hoy = DateTime.Today;
            var admin = await _permisos.EsAdministradorAsync(usuarioId);
            var turnosPropios = await _permisos.TienePermisoAsync(usuarioId, "CLIENTE_TURNO_VER");
            var presupuestosPropios = await _permisos.TienePermisoAsync(usuarioId, "CLIENTE_PRESUPUESTO_VER");
            var ordenes = await _permisos.TienePermisoAsync(usuarioId, "ORDEN_VER");
            var turnos = await _permisos.TienePermisoAsync(usuarioId, "TURNO_VER");
            var facturas = await _permisos.TienePermisoAsync(usuarioId, "FACTURA_VER");
            var pagos = await _permisos.TienePermisoAsync(usuarioId, "PAGO_VER");
            var stock = await _permisos.TienePermisoAsync(usuarioId, "STOCK_ALERTAS");
            ViewData["MostrarResumen"] = admin;
            ViewData["MostrarCliente"] = !admin && (turnosPropios || presupuestosPropios);
            ViewData["MostrarMecanico"] = !admin && ordenes;
            ViewData["MostrarCaja"] = !admin && (turnos || facturas);
            ViewData["MostrarStock"] = stock;
            if (turnosPropios)
                model.TurnosCliente = await _context.Turnos.AsNoTracking().Include(t => t.Vehiculo)
                    .Where(t => t.ClienteId == personaId).ToListAsync();
            if (presupuestosPropios)
                model.PresupuestosPendientes = await _context.Presupuestos.AsNoTracking()
                    .Where(p => p.Estado == EstadoPresupuesto.Pendiente && p.OrdenTrabajo.IngresoVehiculo.Turno.ClienteId == personaId)
                    .Select(p => new MecaniCar360.Models.Presupuesto { Id = p.Id }).ToListAsync();
            if (ordenes)
                model.OrdenesMecanico = await _context.OrdenesTrabajo.AsNoTracking()
                    .Where(o => o.MecanicoId == personaId).ToListAsync();
            if (turnos)
                model.TurnosHoy = await _context.Turnos.AsNoTracking()
                    .Where(t => t.FechaInicio.Date == hoy).ToListAsync();
            if (facturas)
                model.FacturasPendientes = await _context.Facturas.AsNoTracking()
                    .Where(f => f.Estado != EstadoFactura.Anulada &&
                        f.Pagos.Where(p => p.Estado == EstadoPago.Pagado).Sum(p => p.Monto) < f.Total).ToListAsync();
            if (stock)
                model.StockBajo = await _context.Repuestos.AsNoTracking()
                    .Where(r => r.StockActual <= r.StockMinimo).ToListAsync();
            if (admin)
            {
                model.TotalTurnosHoy = model.TurnosHoy.Count;
                model.OrdenesActivas = await _context.OrdenesTrabajo.CountAsync(o =>
                    o.EstadoActual != EstadoOrden.Entregado && o.EstadoActual != EstadoOrden.Finalizado);
            }
            if (pagos)
                model.IngresosHoy = await _context.Pagos.Where(p => p.FechaPago.Date == hoy && p.Estado == EstadoPago.Pagado)
                    .SumAsync(p => (decimal?)p.Monto) ?? 0;
            return View(model);
        }
    }
}
