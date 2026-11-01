using System.Text.RegularExpressions;
using Xunit;

namespace MecaniCar360.Tests;

public class ProgramArranqueConexionTests
{
    [Fact]
    public void Program_ConectaModoAceptacionAlInicializadorProtegido()
    {
        // Complementa las pruebas ejecutables de ArranqueIntegridad sin iniciar
        // Program.Main, que abriria SQL. Detecta la desconexion del argumento.
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "MecaniCar360.csproj")))
            root = root.Parent;
        Assert.NotNull(root);
        var source = File.ReadAllText(Path.Combine(root.FullName, "Program.cs"));
        source = Regex.Replace(source, @"//[^\r\n]*|/\*.*?\*/", "", RegexOptions.Singleline);
        Assert.Single(Regex.Matches(source, @"ArranqueIntegridad\.EjecutarAsync\s*\("));
        Assert.Matches(@"ArranqueIntegridad\.EjecutarAsync\s*\(\s*" +
            @"scope\.ServiceProvider\.GetRequiredService<IntegridadService>\(\),\s*" +
            @"scope\.ServiceProvider\.GetRequiredService<EstadoIntegridad>\(\),\s*" +
            @"\(\)\s*=>\s*InicializadorBD\.Inicializar\(context,\s*builder\.Configuration\),\s*" +
            "omitirInicializacion:\\s*builder\\.Configuration\\.GetValue<bool>\\(\"Arranque:ModoAceptacion\"\\)\\s*\\)", source);
    }
}
