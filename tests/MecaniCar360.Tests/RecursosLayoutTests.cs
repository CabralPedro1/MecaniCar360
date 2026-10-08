using System.Text.RegularExpressions;
using Xunit;

namespace MecaniCar360.Tests;

public class RecursosLayoutTests
{
    [Fact]
    public void ScriptsLocalesDelLayout_ExistenEnWwwroot()
    {
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz != null && !File.Exists(Path.Combine(raiz.FullName, "MecaniCar360.csproj")))
            raiz = raiz.Parent;
        Assert.NotNull(raiz);
        var layout = File.ReadAllText(Path.Combine(raiz.FullName, "Views", "Shared", "_Layout.cshtml"));
        var scripts = Regex.Matches(layout, "<script\\b[^>]*\\bsrc=\"~/([^\"]+)\"");
        Assert.NotEmpty(scripts);
        foreach (Match script in scripts)
        {
            var ruta = script.Groups[1].Value;
            Assert.True(File.Exists(Path.Combine(raiz.FullName, "wwwroot", ruta)),
                $"El layout referencia un script local inexistente: {ruta}");
        }
    }
}
