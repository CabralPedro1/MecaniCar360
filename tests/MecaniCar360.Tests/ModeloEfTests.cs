using MecaniCar360.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Tests;

public class ModeloEfTests
{
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
