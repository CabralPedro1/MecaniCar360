using Xunit;

namespace MecaniCar360.Tests;

public class EnlaceSeguroPresentacionTests
{
    private static string Leer(string ruta)
    {
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz != null && !File.Exists(Path.Combine(raiz.FullName, "MecaniCar360.csproj"))) raiz = raiz.Parent;
        Assert.NotNull(raiz);
        return File.ReadAllText(Path.Combine(raiz!.FullName, ruta));
    }

    [Theory]
    [InlineData("ActivacionCliente/Abrir")]
    [InlineData("ActivacionCliente/Index")]
    [InlineData("ActivacionCliente/Completada")]
    [InlineData("ActivacionCliente/NoDisponible")]
    [InlineData("SeguridadCliente/Abrir")]
    [InlineData("SeguridadCliente/Restablecer")]
    [InlineData("SeguridadCliente/NoDisponible")]
    public void PaginaMantieneAislamientoYRecursosLocales(string pagina)
    {
        var vista = Leer($"Views/{pagina}.cshtml");
        Assert.Contains("Layout = null", vista);
        Assert.Contains("<partial name=\"_EnlaceSeguroHead\"", vista);
        Assert.Contains("<main class=\"enlace-seguro\">", vista);
        Assert.DoesNotContain("style=", vista);
        Assert.DoesNotContain("https://", vista);
        Assert.Contains("no-referrer", vista);
        var head = Leer("Views/Shared/_EnlaceSeguroHead.cshtml");
        Assert.Contains("width=device-width, initial-scale=1.0", head);
        Assert.Contains("~/css/enlace-seguro.css", head);
        Assert.DoesNotContain("<script", head);
    }

    [Theory]
    [InlineData("ActivacionCliente/Index", "ActivacionCliente", "Index")]
    [InlineData("SeguridadCliente/Restablecer", "SeguridadCliente", "Restablecer")]
    public void FormulariosConservanPostTokenOcultoYAntiforgery(string pagina, string controller, string action)
    {
        var vista = Leer($"Views/{pagina}.cshtml");
        Assert.Contains($"asp-controller=\"{controller}\" asp-action=\"{action}\" method=\"post\"", vista);
        Assert.Contains("@Html.AntiForgeryToken()", vista);
        Assert.Contains("<input asp-for=\"Token\" type=\"hidden\"", vista);
        Assert.Contains("if (!ViewData.ModelState.IsValid)", vista);
        Assert.Contains("asp-validation-summary=\"All\" role=\"alert\"", vista);
    }
}
