using MecaniCar360.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Tests;

public class ModeloEfTests
{
    [Theory]
    [InlineData(typeof(MecaniCar360.Models.Factura), "Total")]
    [InlineData(typeof(MecaniCar360.Models.FacturaItem), "PrecioUnitario")]
    [InlineData(typeof(MecaniCar360.Models.OrdenTrabajo), "CostoDiagnostico")]
    [InlineData(typeof(MecaniCar360.Models.Pago), "Monto")]
    [InlineData(typeof(MecaniCar360.Models.Presupuesto), "Total")]
    [InlineData(typeof(MecaniCar360.Models.PresupuestoHistorial), "TotalAnterior")]
    [InlineData(typeof(MecaniCar360.Models.PresupuestoItem), "PrecioUnitario")]
    [InlineData(typeof(MecaniCar360.Models.ProveedorRepuesto), "PrecioCompraActual")]
    [InlineData(typeof(MecaniCar360.Models.Repuesto), "PrecioVenta")]
    public void Importes_ConservanTipoSqlActual(Type entidad, string propiedad)
    {
        using var contexto = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=SoloMetadatos;Integrated Security=true;Connect Timeout=1").Options);
        var campo = contexto.Model.FindEntityType(entidad)?.FindProperty(propiedad);
        Assert.NotNull(campo);
        Assert.Equal("decimal(18,2)", campo.GetRelationalTypeMapping().StoreType);
        Assert.Equal(System.Data.ConnectionState.Closed, contexto.Database.GetDbConnection().State);
    }

    [Fact]
    public void ModeloActual_ContraSnapshot_NoTieneCambiosPendientes()
    {
        // Sólo metadatos. No abre conexión, no ejecuta startup ni lee configuración real.
        using var contexto = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=SoloMetadatos;Integrated Security=true;Connect Timeout=1").Options);
        Assert.False(contexto.Database.HasPendingModelChanges());
        Assert.Equal(System.Data.ConnectionState.Closed, contexto.Database.GetDbConnection().State);
    }
}
