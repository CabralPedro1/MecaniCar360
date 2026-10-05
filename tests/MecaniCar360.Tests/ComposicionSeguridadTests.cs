using MecaniCar360.Services;
using Xunit;

namespace MecaniCar360.Tests;

public class ComposicionSeguridadTests
{
    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(2, 1, true)]
    [InlineData(3, 1, true)]
    [InlineData(3, 2, true)]
    [InlineData(1, 4, false)]
    [InlineData(3, 4, false)]
    public void NuevaArista_RespetaJerarquia(int padre, int hija, bool ciclo)
    {
        var padres = new Dictionary<int, int?> { [1] = null, [2] = 1, [3] = 2, [4] = null };
        Assert.Equal(ciclo, SeguridadService.FormaCiclo(padre, hija, padres));
    }

    [Fact]
    public void GrafoPreviamenteCorrupto_NoRecorreInfinitamente()
    {
        var padres = new Dictionary<int, int?> { [1] = 2, [2] = 1, [3] = null };
        Assert.True(SeguridadService.FormaCiclo(1, 3, padres));
    }
}
