using MecaniCar360.Helpers;
using Xunit;

namespace MecaniCar360.Tests;

public class PasswordValidatorTests
{
    // Todas las entradas son ficticias; no se leen credenciales ni configuración.
    [Theory]
    [InlineData(null, false, "La contraseña es obligatoria.")]
    [InlineData("", false, "La contraseña es obligatoria.")]
    [InlineData("   ", false, "La contraseña es obligatoria.")]
    [InlineData("Aa1!abc", false, "Debe tener al menos 8 caracteres.")]
    [InlineData("aa1!abcd", false, "Debe contener al menos una letra mayúscula.")]
    [InlineData("AA1!ABCD", false, "Debe contener al menos una letra minúscula.")]
    [InlineData("Aa!!abcd", false, "Debe contener al menos un número.")]
    [InlineData("Aa12abcd", false, "Debe contener al menos un símbolo.")]
    [InlineData("Aa1!abcd", true, "")]
    public void EsValida_PrimerRequisitoIncumplido_DevuelveResultadoYMensajeEsperados(
        string? entrada, bool esperado, string errorEsperado)
    {
        var obtenido = PasswordValidator.EsValida(entrada!, out var error);
        Assert.Equal(esperado, obtenido);
        Assert.Equal(errorEsperado, error);
    }
}
