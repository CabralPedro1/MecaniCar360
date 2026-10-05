using System.ComponentModel.DataAnnotations;
using MecaniCar360.Data;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace MecaniCar360.Services;

public sealed class ReportesService(MecaniCarContext db, PermisoService permisos)
{
    public async Task<ServiceResult<ReportesViewModel>> ObtenerAsync(ReporteFiltro filtro, int actor)
    {
        if (!await permisos.TienePermisoAsync(actor, "REPORTES_VER"))
            return ServiceResult<ReportesViewModel>.Error("Acceso denegado.");
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(filtro, new ValidationContext(filtro), errores, true))
            return ServiceResult<ReportesViewModel>.Error("Indique fechas válidas, ordenadas y un período de hasta 367 días.");
        var desde = filtro.Desde!.Value.Date;
        var fin = filtro.Hasta!.Value.Date.AddDays(1);
        // Cohorte por ingreso de OT; estados y avance son actuales, no una reconstrucción al cierre.
        var ordenes = db.OrdenesTrabajo.AsNoTracking().Where(o => o.FechaInicio >= desde && o.FechaInicio < fin);
        var grupos = await ordenes.GroupBy(o => o.EstadoActual).Select(g => new { Estado = g.Key, Cantidad = g.Count() }).ToListAsync();
        var m = new ReportesViewModel {
            Filtro = new ReporteFiltro { Desde = desde, Hasta = fin.AddDays(-1) },
            Ingresadas = grupos.Sum(g => g.Cantidad),
            Finalizadas = await ordenes.CountAsync(o => o.FechaFin != null),
            Entregadas = grupos.Where(g => g.Estado == EstadoOrden.Entregado).Sum(g => g.Cantidad),
            EnReparacion = grupos.Where(g => g.Estado == EstadoOrden.EnReparacion).Sum(g => g.Cantidad),
            Rechazadas = grupos.Where(g => g.Estado == EstadoOrden.Rechazado).Sum(g => g.Cantidad),
            Estados = Enum.GetValues<EstadoOrden>().Select(e => new ReporteEstado(e, grupos.Where(g => g.Estado == e).Sum(g => g.Cantidad))).ToList()
        };
        var facturas = db.Facturas.AsNoTracking().Where(f => f.Estado != EstadoFactura.Anulada && f.FechaEmision >= desde && f.FechaEmision < fin);
        m.Facturas = await facturas.CountAsync();
        m.Facturado = await facturas.SumAsync(f => (decimal?)f.Total) ?? 0;
        // Cobros por fecha de pago, incluso de facturas emitidas fuera del período.
        m.Cobrado = await db.Pagos.Where(p => p.Estado == EstadoPago.Pagado && p.Factura.Estado != EstadoFactura.Anulada &&
            p.FechaPago >= desde && p.FechaPago < fin).SumAsync(p => (decimal?)p.Monto) ?? 0;
        var pagos = db.Pagos.Where(p => p.Estado == EstadoPago.Pagado).GroupBy(p => p.FacturaId)
            .Select(g => new { Id = g.Key, Total = g.Sum(p => p.Monto) });
        var saldos = from f in facturas join p in pagos on f.Id equals p.Id into gp
                     from p in gp.DefaultIfEmpty() select f.Total - ((decimal?)p.Total ?? 0);
        m.Saldo = await saldos.Where(s => s > 0).SumAsync(s => (decimal?)s) ?? 0;
        // Última versión aprobada de trabajos finalizados en el período; no sumar versiones anteriores.
        var versiones = db.PresupuestoVersiones.AsNoTracking().Where(v =>
            v.Decision == EstadoPresupuestoVersion.Aprobado && v.Presupuesto.Estado == EstadoPresupuesto.Aprobado &&
            v.Presupuesto.OrdenTrabajo.FechaFin >= desde && v.Presupuesto.OrdenTrabajo.FechaFin < fin &&
            (v.Presupuesto.OrdenTrabajo.EstadoActual == EstadoOrden.Finalizado || v.Presupuesto.OrdenTrabajo.EstadoActual == EstadoOrden.Entregado) &&
            !db.PresupuestoVersiones.Any(n => n.PresupuestoId == v.PresupuestoId && n.NumeroVersion > v.NumeroVersion));
        var conceptos = await versiones.SelectMany(v => v.Items).Where(i => i.RepuestoId == null)
            .GroupBy(i => i.Descripcion).Select(g => new { Descripcion = g.Key, Cantidad = g.Sum(i => i.Cantidad), Importe = g.Sum(i => i.Cantidad * i.PrecioUnitario) })
            .OrderByDescending(c => c.Cantidad).ThenBy(c => c.Descripcion).ToListAsync();
        m.Conceptos = conceptos.Select(c => new ReporteConcepto(c.Descripcion, c.Cantidad, c.Importe)).ToList();
        var cantidad = m.Conceptos.Sum(c => c.Cantidad);
        foreach (var c in m.Conceptos) c.Porcentaje = cantidad == 0 ? 0 : 100m * c.Cantidad / cantidad;
        return ServiceResult<ReportesViewModel>.Ok(m);
    }
}
