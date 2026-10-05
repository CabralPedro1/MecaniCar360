using System.ComponentModel.DataAnnotations;
using MecaniCar360.Models;
using Xunit;

namespace MecaniCar360.Tests;

public class CalificacionValidacionTests
{
    private static void ComprobarPuntuacion(int puntuacion, bool esperado)
    {
        var modelo = new CalificacionTrabajo { Puntuacion = puntuacion, Fecha = new DateTime(2026, 1, 1) };
        var contexto = new ValidationContext(modelo) { MemberName = nameof(modelo.Puntuacion) };
        var errores = new List<ValidationResult>();
        var obtenido = Validator.TryValidateProperty(modelo.Puntuacion, contexto, errores);
        Assert.Equal(esperado, obtenido);
        if (esperado) Assert.Empty(errores);
        else Assert.Single(errores);
    }

    [Theory]
    [InlineData(-10, false)]
    [InlineData(3, true)]
    [InlineData(10, false)]
    public void Puntuacion_RepresentanteDeClase_ValidaSegunContrato(int puntuacion, bool esperado)
        => ComprobarPuntuacion(puntuacion, esperado);

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void Puntuacion_ValorEnFrontera_ValidaSegunContrato(int puntuacion, bool esperado)
        => ComprobarPuntuacion(puntuacion, esperado);
}
