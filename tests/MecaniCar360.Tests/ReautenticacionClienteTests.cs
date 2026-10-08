using System.Reflection;
using MecaniCar360.Controllers;
using Xunit;

namespace MecaniCar360.Tests;

public sealed class ReautenticacionClienteTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(299, true)]
    [InlineData(301, false)]
    [InlineData(-1, false)]
    public void VinculacionExigePruebaReciente(int antiguedad, bool esperado)
    {
        var ahora = DateTimeOffset.UtcNow;
        var metodo = typeof(GoogleClienteController).GetMethod("ReautenticacionVigente", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.Equal(esperado, metodo.Invoke(null, new object[] { ahora.AddSeconds(-antiguedad).ToUnixTimeSeconds().ToString(), ahora }));
    }
}
