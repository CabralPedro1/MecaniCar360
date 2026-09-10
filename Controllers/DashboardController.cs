using MecaniCar360.Data;
using MecaniCar360.Models.ViewModels;
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

        public DashboardController(MecaniCarContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            int personaId = int.Parse(User.FindFirstValue("PersonaId"));

            var model = new DashboardViewModel();

            if (User.IsInRole("CLIENTE"))
            {
                model.TurnosCliente = await _context.Turnos
                    .Include(t => t.Vehiculo)
                    .Where(t => t.ClienteId == personaId)
                    .ToListAsync();

                model.PresupuestosPendientes = await _context.Presupuestos
                    .Include(p => p.OrdenTrabajo)
                    .ThenInclude(o => o.IngresoVehiculo)
                        .ThenInclude(i => i.Turno)
                    .Where(p => p.Estado == Models.Enums.EstadoPresupuesto.Pendiente &&
                                p.OrdenTrabajo.IngresoVehiculo.Turno.ClienteId == personaId)
                    .ToListAsync();
            }

            if (User.IsInRole("MECANICO"))
            {
                model.OrdenesMecanico = await _context.OrdenesTrabajo
                    .Include(o => o.IngresoVehiculo)
                        .ThenInclude(i => i.Turno)
                    .Include(o => o.Presupuesto)
                    .Where(o => o.MecanicoId == personaId)
                    .ToListAsync();
            }

            if (User.IsInRole("CAJA"))
            {
                var hoy = DateTime.Today;

                model.TurnosHoy = await _context.Turnos
                    .Include(t => t.Vehiculo)
                    .Include(t => t.Cliente)
                    .Where(t => t.FechaInicio.Date == hoy)
                    .ToListAsync();

                model.FacturasPendientes = await _context.Facturas
                    .Include(f => f.Pagos)
                    .Where(f => f.Pagos.Sum(p => p.Monto) < f.Total)
                    .ToListAsync();
            }

            if (User.IsInRole("STOCK"))
            {
                model.StockBajo = await _context.Repuestos
                    .Where(r => r.StockActual <= r.StockMinimo)
                    .ToListAsync();
            }

            if (User.IsInRole("ADMIN"))
            {
                var hoy = DateTime.Today;

                model.TotalTurnosHoy = await _context.Turnos
                    .CountAsync(t => t.FechaInicio.Date == hoy);

                model.OrdenesActivas = await _context.OrdenesTrabajo
                    .CountAsync(o =>
                        o.EstadoActual != Models.Enums.EstadoOrden.Entregado &&
                        o.EstadoActual != Models.Enums.EstadoOrden.Finalizado);

                model.IngresosHoy = await _context.Pagos
                    .Where(p => p.FechaPago.Date == hoy)
                    .SumAsync(p => (decimal?)p.Monto) ?? 0;
            }

            return View(model);
        }
    }
}