using System.Text.RegularExpressions;
using Xunit;

namespace MecaniCar360.Tests;

// Markup regression: does not certify browser interaction or persisted account flows.
public class MiCuentaMenuTests
{
    private static string Layout()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "MecaniCar360.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        return File.ReadAllText(Path.Combine(root.FullName, "Views/Shared/_Layout.cshtml"));
    }

    [Fact]
    public void AccionesPersonales_EstanDentroDeUnUnicoDropdown()
    {
        var layout = Layout();
        Assert.Single(Regex.Matches(layout, "id=\"miCuentaMenu\""));
        Assert.Contains("data-bs-toggle=\"dropdown\" aria-expanded=\"false\" aria-controls=\"miCuentaOpciones\"", layout);
        var start = layout.IndexOf("id=\"miCuentaOpciones\"", StringComparison.Ordinal);
        var menu = layout[start..layout.IndexOf("</ul>", start, StringComparison.Ordinal)];
        foreach (var action in new[] { "MisDatos", "SolicitarEnlace", "CambiarContraseña", "Logout" })
        {
            Assert.Contains($"asp-action=\"{action}\"", menu);
            Assert.Single(Regex.Matches(layout, $"asp-action=\"{action}\""));
        }
        Assert.DoesNotContain("btn-outline-light", menu);
        Assert.Contains("@if (cuentaVisible.EsClienteHabilitado &&", menu);
        Assert.Contains("Permisos.TieneAlgunoAsync(cuentaActor", menu);
        Assert.Contains("@if (cuentaVisible.PuedeGestionarPassword)", menu);
        Assert.Contains("else if (cuentaVisible.PasswordLocal)", menu);
        Assert.Contains("cuentaVisible.PasswordLocal ? \"Cambiar contraseña\" : \"Agregar contraseña\"", menu);
        Assert.DoesNotContain("asp-route-id", menu);
    }

    [Theory]
    [InlineData("SolicitarEnlace")]
    [InlineData("Logout")]
    public void OperacionesSensibles_ConservanPostYAntiforgery(string action)
    {
        var form = Assert.Single(Regex.Matches(Layout(), $"<form[^>]*asp-action=\"{action}\"[^>]*>.*?</form>", RegexOptions.Singleline).Cast<Match>());
        Assert.Contains("method=\"post\"", form.Value);
        Assert.Contains("@Html.AntiForgeryToken()", form.Value);
        Assert.Contains("type=\"submit\"", form.Value);
    }
}
