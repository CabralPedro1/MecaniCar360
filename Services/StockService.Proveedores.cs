using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public partial class StockService
{
    private async Task<Repuesto?> BloquearRepuestoComercialAsync(int id)
    {
        var repuesto = await _context.Repuestos.FromSqlInterpolated(
            $"SELECT * FROM [Repuestos] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").FirstOrDefaultAsync();
        if (repuesto != null) await _context.Entry(repuesto).ReloadAsync();
        return repuesto;
    }

    private async Task<List<ProveedorRepuesto>> RelacionesBloqueadasAsync(int repuestoId)
    {
        var relaciones = await _context.ProveedorRepuestos.FromSqlInterpolated(
            $"SELECT * FROM [ProveedorRepuestos] WITH (UPDLOCK, HOLDLOCK) WHERE [RepuestoId] = {repuestoId}")
            .OrderBy(pr => pr.Id).ToListAsync();
        foreach (var pr in relaciones) await _context.Entry(pr).ReloadAsync();
        return relaciones;
    }

    private async Task<bool> ProveedorOperativoAsync(int id) =>
        // HOLDLOCK retiene la lectura del proveedor hasta commit e impide desactivarlo en medio del alta.
        await _context.Proveedores.FromSqlInterpolated(
            $"SELECT * FROM [Proveedores] WITH (HOLDLOCK) WHERE [Id] = {id}")
            .AsNoTracking().AnyAsync(p => p.Activo);

    private async Task EstablecerPrincipalAsync(List<ProveedorRepuesto> relaciones, ProveedorRepuesto elegido,
        bool principal, int usuarioId)
    {
        var anterior = relaciones.FirstOrDefault(p => p.Principal);
        foreach (var pr in relaciones.Where(p => p.Principal && (p.Id != elegido.Id || !principal)))
            pr.Principal = false;
        // Liberar el valor único antes de asignarlo, sin abandonar la transacción.
        await _context.SaveChangesAsync();
        elegido.Principal = principal;
        if (anterior?.Id != (principal ? (int?)elegido.Id : null))
            _auditoria.RegistrarOperacion("PROVEEDOR_PRINCIPAL_CAMBIADO", "Repuesto", elegido.RepuestoId, usuarioId,
                $"Relación anterior: {anterior?.Id}; nueva: {(principal ? (int?)elegido.Id : null)}.");
    }

    public async Task<ServiceResult> AgregarProveedorARepuestoAsync(int repuestoId, int proveedorId,
        decimal precioCompra, string? codigoProveedor, bool principal, int usuarioSolicitanteId)
    {
        if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");
        if (repuestoId <= 0 || proveedorId <= 0 || !PrecioValido(precioCompra) || codigoProveedor?.Length > 50)
            return ServiceResult.Error("Ingrese IDs, precio positivo con hasta dos decimales y código de hasta 50 caracteres válidos.");
        return await EjecutarStockAsync(async () =>
        {
            var repuesto = await BloquearRepuestoComercialAsync(repuestoId);
            if (repuesto == null || !repuesto.Activo) return ServiceResult.Error("Repuesto no encontrado o inactivo.");
            var relaciones = await RelacionesBloqueadasAsync(repuestoId);
            if (!await ProveedorOperativoAsync(proveedorId)) return ServiceResult.Error("Proveedor no encontrado o inactivo.");
            var relacion = relaciones.SingleOrDefault(p => p.ProveedorId == proveedorId);
            if (relacion?.Activo == true) return ServiceResult.Error("Ese proveedor ya está asociado al repuesto.");
            var reactivada = relacion != null;
            if (relacion == null)
            {
                relacion = new ProveedorRepuesto { RepuestoId = repuestoId, ProveedorId = proveedorId };
                _context.ProveedorRepuestos.Add(relacion);
            }
            relacion.Activo = true;
            relacion.FechaBaja = null;
            relacion.Principal = false;
            relacion.PrecioCompraActual = precioCompra;
            relacion.CodigoProveedor = codigoProveedor?.Trim();
            await _context.SaveChangesAsync();
            if (principal) await EstablecerPrincipalAsync(relaciones, relacion, true, usuarioSolicitanteId);
            _auditoria.RegistrarOperacion("PROVEEDOR_REPUESTO_ASOCIADO", "ProveedorRepuesto", relacion.Id,
                usuarioSolicitanteId, reactivada ? "Relación histórica reactivada." : "Nueva relación comercial.");
            return ServiceResult.Ok(reactivada ? "Relación reactivada conservando su historial." : "Proveedor asociado correctamente.");
        });
    }

    private async Task<ServiceResult> ModificarRelacionAsync(int relacionId, int usuarioId,
        Func<ProveedorRepuesto, List<ProveedorRepuesto>, Task<ServiceResult>> accion)
    {
        if (!await UsuarioAutorizadoAsync(usuarioId, "STOCK_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");
        // Sólo localiza el padre fuera de la transacción: se revalida todo tras bloquearlo.
        var repuestoId = await _context.ProveedorRepuestos.AsNoTracking().Where(p => p.Id == relacionId)
            .Select(p => (int?)p.RepuestoId).FirstOrDefaultAsync();
        if (!repuestoId.HasValue) return ServiceResult.Error("Relación no encontrada.");
        return await EjecutarStockAsync(async () =>
        {
            if (await BloquearRepuestoComercialAsync(repuestoId.Value) == null) return ServiceResult.Error("Repuesto no encontrado.");
            var relaciones = await RelacionesBloqueadasAsync(repuestoId.Value);
            var relacion = relaciones.SingleOrDefault(p => p.Id == relacionId);
            if (relacion == null || !relacion.Activo) return ServiceResult.Error("Relación no encontrada o inactiva.");
            return await accion(relacion, relaciones);
        });
    }

    public Task<ServiceResult> ActualizarPrecioCompraAsync(int proveedorRepuestoId, decimal precio, int usuarioSolicitanteId)
    {
        if (!PrecioValido(precio)) return Task.FromResult(ServiceResult.Error("El precio debe ser positivo y tener hasta dos decimales."));
        return ModificarRelacionAsync(proveedorRepuestoId, usuarioSolicitanteId, async (relacion, _) =>
        {
            if (!await ProveedorOperativoAsync(relacion.ProveedorId))
                return ServiceResult.Error("El proveedor está inactivo; no puede actualizarse el precio comercial.");
            var anterior = relacion.PrecioCompraActual;
            relacion.PrecioCompraActual = precio;
            _auditoria.RegistrarOperacion("PRECIO_COMPRA_ACTUALIZADO", "ProveedorRepuesto", relacion.Id,
                usuarioSolicitanteId, $"Precio anterior: {anterior}; nuevo: {precio}.");
            return ServiceResult.Ok("Precio actualizado correctamente.");
        });
    }

    public Task<ServiceResult> CambiarProveedorPrincipalAsync(int proveedorRepuestoId, int usuarioSolicitanteId) =>
        ModificarRelacionAsync(proveedorRepuestoId, usuarioSolicitanteId, async (relacion, relaciones) =>
        {
            if (!await ProveedorOperativoAsync(relacion.ProveedorId)) return ServiceResult.Error("El proveedor está inactivo.");
            await EstablecerPrincipalAsync(relaciones, relacion, true, usuarioSolicitanteId);
            return ServiceResult.Ok("Proveedor principal actualizado.");
        });

    public Task<ServiceResult> EliminarProveedorDelRepuestoAsync(int proveedorRepuestoId, int usuarioSolicitanteId) =>
        ModificarRelacionAsync(proveedorRepuestoId, usuarioSolicitanteId, async (relacion, relaciones) =>
        {
            if (relacion.Principal) await EstablecerPrincipalAsync(relaciones, relacion, false, usuarioSolicitanteId);
            relacion.Activo = false;
            relacion.FechaBaja = DateTime.UtcNow;
            relacion.Principal = false;
            _auditoria.RegistrarOperacion("PROVEEDOR_REPUESTO_DESVINCULADO", "ProveedorRepuesto", relacion.Id, usuarioSolicitanteId);
            return ServiceResult.Ok("Relación desvinculada; historial conservado.");
        });
}
