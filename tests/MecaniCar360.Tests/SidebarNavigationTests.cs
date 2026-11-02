using System.Text.RegularExpressions;
using Xunit;
namespace MecaniCar360.Tests;

// Structural regression only: browser and effective role validation remain separate.
public class SidebarNavigationTests
{
    private static string Source(string path)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "MecaniCar360.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        return File.ReadAllText(Path.Combine(root.FullName, path));
    }
    [Theory]
    [InlineData("Account", "Inicio")]
    [InlineData("Notificacion", "Index")]
    [InlineData("SeguridadCliente", "SolicitarEnlace")]
    [InlineData("Account", "CambiarContraseña")]
    [InlineData("Account", "Logout")]
    [InlineData("Dashboard", "Index")]
    [InlineData("PortalCliente", "Index")]
    [InlineData("Inicio", "Caja")]
    [InlineData("Inicio", "Mecanico")]
    [InlineData("Inicio", "Stock")]
    [InlineData("Turno", "Index")]
    [InlineData("Persona", "Clientes")]
    [InlineData("OrdenTrabajo", "Index")]
    [InlineData("Stock", "Index")]
    [InlineData("Marca", "Index")]
    [InlineData("Usuario", "Administracion")]
    [InlineData("Factura", "Index")]
    [InlineData("Factura", "MisFacturas")]
    [InlineData("Garantia", "Index")]
    [InlineData("Auditoria", "Index")]
    [InlineData("Reportes", "Index")]
    public void ExistingDestinationAppearsOnce(string controller, string action)
    {
        var layout = Source("Views/Shared/_Layout.cshtml");
        Assert.Single(Regex.Matches(layout, $"asp-controller=\"{controller}\"\\s+asp-action=\"{action}\""));
    }
    [Fact]
    public void HeaderKeepsMenuFirstAndBrandInIndependentCenteredColumn()
    {
        var layout = Source("Views/Shared/_Layout.cshtml");
        Assert.True(layout.IndexOf("id=\"navigationToggle\"") < layout.IndexOf("class=\"navbar-brand\""));
        Assert.True(layout.IndexOf("class=\"navbar-brand\"") < layout.IndexOf("class=\"mc-header-actions\""));
        var css = Source("Views/Shared/_Layout.cshtml.css");
        Assert.Contains("grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr)", css);
        Assert.Contains("grid-column: 1 / -1; grid-row: 2", css);
        Assert.Contains(".mc-header-menu", css);
    }

    [Fact]
    public void SidebarUsesOneOffcanvasAndNativeKeyboardGroups()
    {
        var layout = Source("Views/Shared/_Layout.cshtml");
        Assert.DoesNotContain("asp-controller=\"Seguridad\"", layout);
        Assert.DoesNotContain("asp-controller=\"Rol\"", layout);
        Assert.Single(Regex.Matches(layout, "id=\"navigationSidebar\""));
        Assert.Contains("data-bs-target=\"#navigationSidebar\"", layout);
        Assert.Contains("aria-controls=\"navigationSidebar\" id=\"navigationToggle\" aria-expanded=\"false\"", layout);
        Assert.DoesNotContain("<details class=\"mc-nav-group\" open>", layout);
        Assert.DoesNotContain("visibility: visible !important", Source("Views/Shared/_Layout.cshtml.css"));
        Assert.DoesNotContain("grid-template-columns: 17rem", Source("wwwroot/css/site.css"));
        var bootstrap = Source("wwwroot/lib/bootstrap/dist/js/bootstrap.js");
        Assert.Contains("Escape", bootstrap);
        Assert.DoesNotContain("data-bs-keyboard=\"false\"", layout);
        Assert.DoesNotContain("data-bs-backdrop=\"false\"", layout);
        Assert.Equal(5, Regex.Matches(layout, "<summary>").Count);
        Assert.Contains("data-bs-dismiss=\"offcanvas\"", layout);
        Assert.Contains("asp-action=\"Logout\" method=\"post\"", layout);
        Assert.Contains("@Html.AntiForgeryToken()", layout);
        var script = Source("wwwroot/js/navigation.js");
        Assert.Contains("aria-current", script);
        Assert.Contains("bootstrap.Offcanvas.getInstance(sidebar)?.hide()", script);
    }
}
