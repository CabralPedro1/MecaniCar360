using System.Globalization;
using System.Reflection;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Tests;

public class NormalizacionEmailTests
{
    private static readonly Func<string, string> Clave = typeof(IdentificadorCuenta)
        .GetMethod("ClaveEmailInvariante", BindingFlags.Static | BindingFlags.NonPublic)!
        .CreateDelegate<Func<string, string>>();

    [Theory]
    [InlineData("es-AR")]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    public void Clave_ConservaSemanticaAnteriorYEquivalencias(string cultura)
    {
        var anterior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultura);
            foreach (var email in new[] { "cliente@example.invalid", " CLIENTE@EXAMPLE.INVALID ", "", "  ", "\tcliente@example.invalid\r\n", "iñaki@example.invalid" })
                Assert.Equal(email.Trim().ToUpperInvariant(), Clave(email));
            Assert.Equal(Clave("cliente@example.invalid"), Clave(" CLIENTE@EXAMPLE.INVALID "));
            Assert.NotEqual(Clave("otro@example.invalid"), Clave("cliente@example.invalid"));
            Assert.Equal("", Clave(IdentificadorCuenta.Normalizar(null)));
            Assert.Throws<NullReferenceException>(() => Clave(null!));
        }
        finally { CultureInfo.CurrentCulture = anterior; }
    }

    [Theory]
    [InlineData("cliente@example.invalid")]
    [InlineData(" CLIENTE@EXAMPLE.INVALID ")]
    public void ComparacionDuplicados_ConservaSqlYFiltroDeNulos(string entrada)
    {
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=SoloMetadatos;Integrated Security=true;Connect Timeout=1").Options);
        (string Personas, string Usuarios) Sql(string email) => (
            db.Personas.Where(p => p.Email != null && p.Email.Trim().ToUpper() == email).ToQueryString(),
            db.Usuarios.Where(u => u.EmailLogin.Trim().ToUpper() == email).ToQueryString());
        Assert.Equal(Sql(entrada.Trim().ToUpperInvariant()), Sql(Clave(entrada)));
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }
}
