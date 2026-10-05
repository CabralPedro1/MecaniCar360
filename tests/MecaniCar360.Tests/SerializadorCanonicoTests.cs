using System.Globalization;
using MecaniCar360.Data.Integridad;
using Xunit;

namespace MecaniCar360.Tests;

public class SerializadorCanonicoTests
{
    [Fact]
    public void Token_Null_ProduceMarcadorNulo()
        => Assert.Equal("N;", SerializadorCanonico.Token(null));

    [Fact]
    public void Token_Vacio_SeDistingueDeNull()
    {
        Assert.Equal("V0:;", SerializadorCanonico.Token(""));
        Assert.NotEqual(SerializadorCanonico.Dvh("T", new object?[] { null }),
            SerializadorCanonico.Dvh("T", new object?[] { "" }));
    }

    [Fact]
    public void Dvh_DelimitadoresEnCampos_NoConfundeParticiones()
    {
        Assert.Equal("V5:a|b;c;", SerializadorCanonico.Token("a|b;c"));
        Assert.NotEqual(SerializadorCanonico.Dvh("T", new object?[] { "a|b", "c" }),
            SerializadorCanonico.Dvh("T", new object?[] { "a", "b|c" }));
    }

    [Fact]
    public void Token_TextoMultibyte_CuentaBytesUtf8()
        => Assert.Equal("V6:ñ🚗;", SerializadorCanonico.Token("ñ🚗"));

    [Fact]
    public void Token_Decimal_UsaDosDecimalesInvariantes()
    {
        Assert.Equal("V5:12.50;", SerializadorCanonico.Token(12.5m));
        Assert.Equal(SerializadorCanonico.Token(12.50m), SerializadorCanonico.Token(12.5000m));
    }

    [Fact]
    public void Token_Fecha_ConservaPrecisionSinConvertirZona()
    {
        var fecha = new DateTime(2026, 1, 2, 3, 4, 5).AddTicks(1234567);
        Assert.Equal("V27:2026-01-02T03:04:05.1234567;", SerializadorCanonico.Token(fecha));
        Assert.Equal(SerializadorCanonico.Token(fecha), SerializadorCanonico.Token(DateTime.SpecifyKind(fecha, DateTimeKind.Utc)));
    }

    [Fact]
    public void Dvh_CulturasEsArYEnUs_ProduceMismoHash()
    {
        var anterior = CultureInfo.CurrentCulture;
        try
        {
            object?[] valores = { 7, 12.50m, new DateTime(2026, 1, 2), "ñ" };
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-AR");
            var argentino = SerializadorCanonico.Dvh("T", valores);
            Assert.Equal("V5:12.50;", SerializadorCanonico.Token(12.50m));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.Equal(argentino, SerializadorCanonico.Dvh("T", valores));
        }
        finally { CultureInfo.CurrentCulture = anterior; }
    }

    [Fact]
    public void Dvh_VectorVersionUno_CoincideConSha256Conocido()
    {
        // SHA256 del literal MC360|DVH|1|V8:Personas;|V1:7;|V3:Ana;
        const string esperado = "6867AA966479BBB0F0B3E3A78EF83E9A19C836D38C2EC70626B8E6A0FC90B757";
        Assert.Equal(esperado, SerializadorCanonico.Dvh("Personas", new object?[] { 7, "Ana" }));
        Assert.Equal(esperado, SerializadorCanonico.Dvh("Personas", new object?[] { 7, "Ana" }));
    }

    [Fact]
    public void Dvh_CambioDeClave_CambiaHash()
        => Assert.NotEqual(SerializadorCanonico.Dvh("Personas", new object?[] { 7, "Ana" }),
            SerializadorCanonico.Dvh("Personas", new object?[] { 8, "Ana" }));

    [Fact]
    public void Dvv_TablaVacia_CoincideConVectorConocido()
        => Assert.Equal("F5FC3D419FF572681E605BA0587A299F274B1B0EED4C6791DAE79D7716839D6A",
            SerializadorCanonico.Dvv("Personas", 0, Array.Empty<(object?[], string?)>()));

    [Fact]
    public void Dvv_ClaveCompuestaOCantidadDistinta_CambiaHash()
    {
        var filas = new[] { (new object?[] { 1, 23 }, (string?)new string('A', 64)) };
        var otras = new[] { (new object?[] { 12, 3 }, (string?)new string('A', 64)) };
        var hash = SerializadorCanonico.Dvv("PersonaRoles", 1, filas);
        Assert.Equal(hash, SerializadorCanonico.Dvv("PersonaRoles", 1, filas));
        Assert.NotEqual(hash, SerializadorCanonico.Dvv("PersonaRoles", 1, otras));
        Assert.NotEqual(hash, SerializadorCanonico.Dvv("PersonaRoles", 2, filas));
    }
}
