using MecaniCar360.Middleware;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MecaniCar360.Tests;

public sealed class ArranqueAceptacionTests : IDisposable
{
    private readonly string directorio = Path.Combine(Path.GetTempPath(), "MC360-arranque-" + Guid.NewGuid().ToString("N"));
    private EstadoIntegridad Estado() => new(directorio, Path.Combine(directorio, "wwwroot"));
    private static ResultadoIntegridad Resultado(bool valida) => new(
        valida ? CondicionIntegridad.Valida : CondicionIntegridad.Comprometida,
        DateTime.UtcNow, 1, null, valida ? Array.Empty<ErrorIntegridad>() :
        new[] { new ErrorIntegridad("Prueba", null, "DVH_INVALIDO") });

    [Fact]
    public async Task Normal_PorDefecto_VerificaInicializaYVerifica()
    {
        var pasos = new List<string>();
        var estado = Estado();
        await ArranqueIntegridad.EjecutarAsync(() =>
        {
            pasos.Add("verificar");
            return Task.FromResult(Resultado(true));
        }, estado, () => pasos.Add("inicializar"));
        Assert.Equal(new[] { "verificar", "inicializar", "verificar" }, pasos);
        Assert.True(estado.Resultado.EsValida);
        Assert.False(new ConfigurationBuilder().Build().GetValue<bool>("Arranque:ModoAceptacion"));
    }

    [Fact]
    public async Task Aceptacion_NoInvocaInicializador_YConservaAmbasVerificaciones()
    {
        var estado = Estado();
        var llamadas = 0;
        await ArranqueIntegridad.EjecutarAsync(() =>
        {
            llamadas++;
            return Task.FromResult(Resultado(true));
        }, estado, () => throw new InvalidOperationException("No debe ejecutar migración/seed"), true);
        Assert.Equal(2, llamadas);
        Assert.True(estado.Resultado.EsValida);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public async Task IntegridadInvalida_BloqueaPipeline_EnAmbosModos(bool aceptacion, int fallaEn)
    {
        var estado = Estado();
        var lecturas = 0;
        var inicializaciones = 0;
        await ArranqueIntegridad.EjecutarAsync(() => Task.FromResult(Resultado(++lecturas != fallaEn)),
            estado, () => inicializaciones++, aceptacion);
        Assert.Equal(fallaEn, lecturas);
        Assert.Equal(!aceptacion && fallaEn == 2 ? 1 : 0, inicializaciones);
        Assert.Equal(CondicionIntegridad.Comprometida, estado.Resultado.Estado);
        var siguiente = false;
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        await new IntegridadMiddleware(_ => { siguiente = true; return Task.CompletedTask; }).InvokeAsync(http, estado);
        Assert.False(siguiente);
        Assert.Equal(503, http.Response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ErrorSegundaVerificacion_NoHabilitaSistema(bool aceptacion)
    {
        var estado = Estado();
        var llamadas = 0;
        await ArranqueIntegridad.EjecutarAsync(() => ++llamadas == 1
            ? Task.FromResult(Resultado(true)) : throw new InvalidOperationException("Fallo simulado"),
            estado, () => { }, aceptacion);
        Assert.Equal(CondicionIntegridad.Comprometida, estado.Resultado.Estado);
    }

    [Fact]
    public async Task Normal_ErrorInicializacion_NoEjecutaVerificacionPosteriorNiHabilita()
    {
        var estado = Estado();
        var llamadas = 0;
        await ArranqueIntegridad.EjecutarAsync(() => { llamadas++; return Task.FromResult(Resultado(true)); },
            estado, () => throw new InvalidOperationException("Fallo simulado"));
        Assert.Equal(1, llamadas);
        Assert.Equal(CondicionIntegridad.Comprometida, estado.Resultado.Estado);
    }

    public void Dispose()
    {
        if (Directory.Exists(directorio)) Directory.Delete(directorio, true);
    }
}
