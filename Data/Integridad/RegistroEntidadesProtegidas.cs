using MecaniCar360.Models;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Data.Integridad;

public sealed record EntidadProtegida(Type Tipo, string Tabla, string[] Claves, string[] Campos);
public static class RegistroEntidadesProtegidas
{
    public const int Version = 1;
    public static readonly IReadOnlyList<EntidadProtegida> Todas = Array.AsReadOnly(new[]
    {
        E<Usuario>("Usuarios", "Id", "Id,Username,EmailLogin,PasswordHash,SecurityStamp,Activo,PrimerLogin,PersonaId,FechaCreacion"),
        E<Persona>("Personas", "Id", "Id,Nombre,Apellido,Dni,Telefono,Email,Activo"),
        E<Rol>("Roles", "Id", "Id,Nombre,EsRolCliente,FechaCreacion,Activo"),
        E<PersonaRol>("PersonaRoles", "PersonaId,RolId", "PersonaId,RolId,FechaAlta,FechaBaja,OtorgadoPorUsuarioId"),
        E<Factura>("Facturas", "Id", "Id,OrdenTrabajoId,PresupuestoOrigenId,PresupuestoVersionOrigenId,Estado,Total,FechaEmision,NumeroFactura,Observaciones,LinkPago,QRPago"),
        E<FacturaItem>("FacturaItems", "Id", "Id,FacturaId,Descripcion,Cantidad,PrecioUnitario"),
        E<Pago>("Pagos", "Id", "Id,FacturaId,Monto,FechaPago,MetodoPago,Estado,RegistradoPorUsuarioId"),
        E<Repuesto>("Repuestos", "Id", "Id,SKU,Nombre,Marca,Modelo,Compatibilidad,PrecioVenta,StockActual,StockMinimo,Activo,FechaCreacion"),
        E<LoteRepuesto>("LotesRepuesto", "Id", "Id,CodigoLote,RepuestoId,ProveedorRepuestoId,CantidadIngresada,CantidadDisponible,PrecioCompra,FechaIngreso"),
        E<MovimientoStock>("MovimientosStock", "Id", "Id,RepuestoId,ProveedorRepuestoId,Cantidad,Fecha,Tipo,RealizadoPorUsuarioId,OrdenTrabajoId,Observaciones"),
        E<MovimientoStockLote>("MovimientosStockLote", "MovimientoStockId,LoteRepuestoId", "MovimientoStockId,LoteRepuestoId,Cantidad"),
        E<Auditoria>("Auditorias", "Id", "Id,UsuarioId,Accion,Entidad,EntidadId,Descripcion,Fecha")
    });
    private static EntidadProtegida E<T>(string tabla, string claves, string campos) => new(typeof(T), tabla, claves.Split(','), campos.Split(','));
    public static void Configurar(ModelBuilder model)
    {
        foreach (var e in Todas)
        {
            model.Entity(e.Tipo).Property<string>("DVH").HasColumnType("varchar(64)").HasMaxLength(64).IsRequired(false);
            model.Entity(e.Tipo).ToTable(e.Tabla, t => t.HasCheckConstraint("CK_" + e.Tabla + "_DVH",
                "[DVH] IS NULL OR (DATALENGTH([DVH]) = 64 AND [DVH] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%')"));
        }
        model.Entity<DigitoVerificadorVertical>(e =>
        {
            e.ToTable("DigitosVerificadoresVerticales", t =>
            {
                t.HasCheckConstraint("CK_DVV_Valor", "DATALENGTH([Valor]) = 64 AND [Valor] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%'");
                t.HasCheckConstraint("CK_DVV_Version", "[VersionAlgoritmo] > 0");
                t.HasCheckConstraint("CK_DVV_Cantidad", "[CantidadRegistros] >= 0");
            });
            e.HasKey(x => x.NombreEntidad);
            e.Property(x => x.NombreEntidad).HasMaxLength(128);
            e.Property(x => x.Valor).HasColumnType("varchar(64)").HasMaxLength(64).IsRequired();
            e.Property(x => x.FechaActualizacionUtc).HasColumnType("datetime2(7)");
        });
    }
}
