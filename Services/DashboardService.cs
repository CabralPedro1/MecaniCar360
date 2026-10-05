using MecaniCar360.Data;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class DashboardService(MecaniCarContext db, PermisoService permisos, TimeProvider reloj)
{
    public async Task<ServiceResult<DashboardViewModel>> ObtenerAsync(int actor)
    {
        if (!await permisos.TienePermisoAsync(actor, "DASHBOARD_VER") ||
            !(await permisos.ObtenerPersonaActivaIdAsync(actor)).HasValue)
            return ServiceResult<DashboardViewModel>.Error("Acceso denegado.");
        var hoy = reloj.GetLocalNow().Date;
        var manana = hoy.AddDays(1);
        var mes = new DateTime(hoy.Year, hoy.Month, 1);
        var siguiente = mes.AddMonths(1);
        var inicio = mes.AddMonths(-5);
        var ordenes = db.OrdenesTrabajo.AsNoTracking();
        var facturas = db.Facturas.AsNoTracking().Where(f => f.Estado != EstadoFactura.Anulada);
        var model = new DashboardViewModel
        {
            TurnosHoy = await db.Turnos.CountAsync(t => t.FechaInicio >= hoy && t.FechaInicio < manana),
            EnTaller = await db.IngresosVehiculo.Where(i => i.FechaEgreso == null).Select(i => i.VehiculoId).Distinct().CountAsync(),
            // Activas: trabajo técnico aún no finalizado ni rechazado.
            OrdenesActivas = await ordenes.CountAsync(o => o.EstadoActual == EstadoOrden.Pendiente ||
                o.EstadoActual == EstadoOrden.Diagnostico || o.EstadoActual == EstadoOrden.EsperandoAprobacion ||
                o.EstadoActual == EstadoOrden.Aprobado || o.EstadoActual == EstadoOrden.EnReparacion),
            EsperandoAprobacion = await ordenes.CountAsync(o => o.EstadoActual == EstadoOrden.EsperandoAprobacion),
            EnReparacion = await ordenes.CountAsync(o => o.EstadoActual == EstadoOrden.EnReparacion),
            PendientesEntrega = await ordenes.CountAsync(o => o.EstadoActual == EstadoOrden.Finalizado && o.IngresoVehiculo.FechaEgreso == null),
            FacturadoMes = await facturas.Where(f => f.FechaEmision >= mes && f.FechaEmision < siguiente).SumAsync(f => (decimal?)f.Total) ?? 0
        };
        // Agregación en SQL; sólo los saldos positivos de facturas no anuladas.
        var pagosPorFactura = db.Pagos.Where(p => p.Estado == EstadoPago.Pagado)
            .GroupBy(p => p.FacturaId).Select(g => new { FacturaId = g.Key, Total = g.Sum(p => p.Monto) });
        var saldos = from factura in facturas
                     join pago in pagosPorFactura on factura.Id equals pago.FacturaId into pagos
                     from pago in pagos.DefaultIfEmpty()
                     select factura.Total - ((decimal?)pago.Total ?? 0);
        model.SaldoPendiente = await saldos.Where(s => s > 0).SumAsync(s => (decimal?)s) ?? 0;
        var grupos = await ordenes.Where(o => o.FechaInicio >= inicio && o.FechaInicio < siguiente)
            .GroupBy(o => new { o.FechaInicio.Year, o.FechaInicio.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Cantidad = g.Count() }).ToListAsync();
        model.Meses = Enumerable.Range(0, 6).Select(n => inicio.AddMonths(n))
            .Select(m => new DashboardMes(m, grupos.SingleOrDefault(g => g.Year == m.Year && g.Month == m.Month)?.Cantidad ?? 0)).ToList();
        if (await permisos.TienePermisoAsync(actor, "AUDITORIA_VER"))
            model.Actividad = await db.Auditorias.AsNoTracking()
                .Where(a => a.Entidad != "Sesion")
                .OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id).Take(8)
                .Select(a => new AuditoriaConsultaViewModel { Fecha = a.Fecha, Actor = a.Usuario == null ? null : a.Usuario.Username,
                    Accion = a.Accion, Entidad = a.Entidad, EntidadId = a.EntidadId }).ToListAsync();
        foreach (var a in new[] {
            ("Turnos", "Turno", "Index", "TURNO_VER"), ("Órdenes", "OrdenTrabajo", "Index", "ORDEN_VER"),
            ("Clientes", "Persona", "Clientes", "PERSONA_VER"), ("Stock", "Stock", "Index", "STOCK_VER"),
            ("Facturación", "Factura", "Index", "FACTURA_VER"), ("Garantías", "Garantia", "Index", "GARANTIA_VER"),
            ("Auditoría", "Auditoria", "Index", "AUDITORIA_VER") })
            if (await permisos.TienePermisoAsync(actor, a.Item4))
                model.Accesos.Add(new DashboardAcceso(a.Item1, a.Item2, a.Item3));
        return ServiceResult<DashboardViewModel>.Ok(model);
    }
}
