using Xunit;

namespace MecaniCar360.Tests;

// Regresión estática del arreglo de datos; no sustituye la restricción ejecutada en SQL.
public class InvitacionesConcurrencyVigenciaTests
{
    [Fact]
    public void FixtureDerivaExpiracionDeLaMismaCreacionUtc()
    {
        var source = ReadHarness("ConcurrencyFixtures.cs");
        Assert.Contains("var ahora = DateTime.UtcNow;", source);
        Assert.Contains("var creacionInvitacion = ahora.AddHours(-1);", source);
        Assert.Contains("FechaCreacion=creacionInvitacion,", source);
        Assert.Contains("FechaExpiracion=creacionInvitacion.AddHours(48), FechaConsumida=ahora", source);
    }

    [Fact]
    public void SimulacionDeIntervaloRecalculaExpiracionAntesDeGuardar()
    {
        var source = ReadHarness("SqlConcurrencyTests.CorreosCuenta.cs");
        var start = source.IndexOf("invitation.FechaCreacion =", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var save = source.IndexOf("await s.Db.SaveChangesAsync();", start, StringComparison.Ordinal);
        Assert.True(save > start);
        Assert.Contains("invitation.FechaExpiracion = invitation.FechaCreacion.AddHours(48);", source[start..save]);
    }

    private static string ReadHarness(string file)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "MecaniCar360.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        return File.ReadAllText(Path.Combine(root.FullName, "tests", "MecaniCar360.ConcurrencyTests", file));
    }
}
