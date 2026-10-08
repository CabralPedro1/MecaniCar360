using System.Text.RegularExpressions;
using Xunit;

namespace MecaniCar360.Tests;

public class TablasResponsiveTests
{
    private static string Leer(string vista)
    {
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz != null && !File.Exists(Path.Combine(raiz.FullName, "MecaniCar360.csproj"))) raiz = raiz.Parent;
        Assert.NotNull(raiz);
        return File.ReadAllText(Path.Combine(raiz!.FullName, "Views", vista + ".cshtml"));
    }

    [Theory]
    [InlineData("Presupuesto/Detalle")]
    [InlineData("Presupuesto/Propio")]
    [InlineData("Stock/Detalle")]
    [InlineData("Stock/Movimientos")]
    [InlineData("Stock/AdministrarProveedores")]
    [InlineData("Marca/Index")]
    [InlineData("Marca/Detalle")]
    [InlineData("Modelo/Index")]
    [InlineData("Rol/Index")]
    [InlineData("Persona/AdministrarRoles")]
    [InlineData("Usuario/PersonaCuenta")]
    [InlineData("Proveedor/Detalle")]
    [InlineData("OrdenTrabajo/MecanicosDisponibles")]
    [InlineData("OrdenTrabajo/Detalle")]
    [InlineData("Shared/_Evidencias")]
    public void CadaTablaTieneUnContenedorDesplazableAccesible(string vista)
    {
        var contenido = Leer(vista);
        var tablas = Regex.Matches(contenido, "<table\\b").Count;
        Assert.True(tablas > 0);
        Assert.Equal(tablas, Regex.Matches(contenido,
            "<div class=\"table-responsive\" role=\"region\" aria-label=\"[^\"]+\" tabindex=\"0\"><table\\b").Count);
        Assert.Equal(tablas, Regex.Matches(contenido, "</table></div>").Count);
        Assert.Equal(tablas, Regex.Matches(contenido, "class=\"table-responsive\"").Count);
    }

    [Theory]
    [InlineData("Familia")]
    [InlineData("Rol")]
    [InlineData("Index")]
    public void SeguridadPermiteApilarYEnvolverFilas(string vista)
    {
        var contenido = Leer("Seguridad/" + vista);
        Assert.Contains("flex-column flex-sm-row flex-wrap", contenido);
        Assert.DoesNotContain("class=\"d-flex gap-2", contenido);
    }
}
