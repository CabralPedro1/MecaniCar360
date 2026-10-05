using MecaniCar360.Data;
using MecaniCar360.Models.Enums;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class InicioOperativoService(PermisoService permisos, TurnoService turnos,
    OrdenTrabajoService ordenes, MecaniCarContext db)
{
    public async Task<InicioOperativoViewModel?> ObtenerAsync(string area, int actor)
    {
        var patente = area switch { "Caja" => "TURNO_VER", "Mecanico" => "ORDEN_MODIFICAR", "Stock" => "STOCK_VER", _ => "" };
        if (patente == "" || !await permisos.TienePermisoAsync(actor, patente)) return null;
        var model = new InicioOperativoViewModel { Titulo = area switch { "Caja" => "Mostrador y caja", "Mecanico" => "Mi trabajo en el taller", _ => "Inventario y abastecimiento" } };
        async Task Acceso(string capacidad, string texto, string controller, string action = "Index")
        {
            if (await permisos.TienePermisoAsync(actor, capacidad)) model.Accesos.Add(new(texto, controller, action));
        }
        if (area == "Caja")
        {
            var r = await turnos.ObtenerTurnosDeFechaAsync(DateTime.Today, actor);
            if (r.Exitoso) { model.Turnos = r.Data!; model.VerTurnos = true; }
            await Acceso("TURNO_VER", "Agenda y turnos", "Turno");
            await Acceso("INGRESO_VER", "Recepción / vehículos en taller", "IngresoVehiculo", "EnTaller");
            await Acceso("PERSONA_VER", "Clientes", "Persona", "Clientes");
            await Acceso("VEHICULO_VER", "Vehículos", "Vehiculo");
            await Acceso("ORDEN_VER", "Seguimiento y entregas", "OrdenTrabajo");
            await Acceso("FACTURA_VER", "Facturas y pagos", "Factura");
            await Acceso("GARANTIA_VER", "Garantías", "Garantia");
            if (await permisos.TienePermisoAsync(actor, "ORDEN_VER"))
            {
                var os = await ordenes.ObtenerTodasAsync(actor);
                if (os.Exitoso) { model.VerOrdenes = true; model.Ordenes = os.Data!.Where(o => o.EstadoActual is EstadoOrden.Finalizado or EstadoOrden.Rechazado).ToList(); }
            }
        }
        if (area == "Mecanico")
        {
            var persona = await permisos.ObtenerPersonaActivaIdAsync(actor);
            if (persona.HasValue && await permisos.TienePermisoAsync(actor, "ORDEN_VER"))
            {
                var r = await ordenes.ObtenerDeMecanicoAsync(persona.Value, actor);
                if (r.Exitoso) { model.Ordenes = r.Data!; model.VerOrdenes = true; }
            }
            await Acceso("ORDEN_VER", "Consultar órdenes", "OrdenTrabajo");
        }
        if (area == "Stock")
        {
            model.VerExistencias = true;
            // Proyección técnica: no carga precios de compra ni datos comerciales de proveedores.
            model.Existencias = await db.Repuestos.AsNoTracking().Where(r => r.Activo)
                .OrderBy(r => r.StockActual > r.StockMinimo).ThenBy(r => r.Nombre)
                .Select(r => new ExistenciaOperativa(r.Id, r.Nombre, r.StockActual, r.StockMinimo)).ToListAsync();
            await Acceso("STOCK_VER", "Repuestos y lotes", "Stock");
            if (await permisos.TienePermisoAsync(actor, "PROVEEDOR_VER"))
            {
                await Acceso("STOCK_MOVIMIENTO", "Movimientos", "Stock", "Movimientos");
                await Acceso("PROVEEDOR_VER", "Proveedores", "Proveedor");
            }
        }
        model.VerDetalleOrden = await permisos.TienePermisoAsync(actor, "ORDEN_VER_DETALLE");
        await Acceso("NOTIFICACION_VER", "Notificaciones", "Notificacion");
        return model;
    }
}
