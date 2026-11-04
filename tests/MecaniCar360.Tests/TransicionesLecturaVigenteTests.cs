using Xunit;

namespace MecaniCar360.Tests;

// Contract checks only: actual tracking and races are covered by opt-in SQL tests.
public class TransicionesLecturaVigenteTests
{
    [Theory]
    [InlineData("DiagnosticoService.cs", "IniciarAsync(", "ObtenerAsync(")]
    [InlineData("OrdenTrabajoService.cs", "IniciarReparacionAsync(", "FinalizarAsync(")]
    [InlineData("OrdenTrabajoService.cs", "FinalizarAsync(", "EntregarAsync(")]
    public void ValidacionesOcurrenDespuesDeBloqueoYRecarga(string file, string start, string end)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "MecaniCar360.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        var source = File.ReadAllText(Path.Combine(root.FullName, "Services", file));
        var a = source.IndexOf(start, StringComparison.Ordinal);
        var b = source.IndexOf(end, a + start.Length, StringComparison.Ordinal);
        var method = source[a..b];
        var position = 0;
        foreach (var step in new[] { "BeginTransactionAsync(System.Data.IsolationLevel.Serializable)",
            "WITH (UPDLOCK, HOLDLOCK)", "ReloadAsync()", "EntityState.Detached",
            "orden.FechaFin.HasValue", "_ordenStateService.CambiarEstado(", "SaveChangesAsync()", "CommitAsync()" })
        {
            var next = method.IndexOf(step, position, StringComparison.Ordinal);
            Assert.True(next >= position, $"Missing or unordered: {step}");
            position = next + step.Length;
        }
    }
}
