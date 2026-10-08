using MecaniCar360.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace MecaniCar360.Tests;

public class PermisosVistaTests
{
    [Fact]
    public async Task ComprobacionesSolapadasCompartenLaResolucionEnCurso()
    {
        var llamadas = 0;
        var pendiente = new TaskCompletionSource<PermisosVisuales>(TaskCreationOptions.RunContinuationsAsynchronously);
        var contexto = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var permisos = new PermisosVista(_ => { llamadas++; return pendiente.Task; }, contexto);
        var consultas = Enumerable.Range(0, 20).Select(_ => permisos.TienePermisoAsync(1, "STOCK_VER")).ToArray();
        Assert.Equal(1, llamadas);
        pendiente.SetResult(new(false, new[] { "STOCK_VER" }));
        Assert.All(await Task.WhenAll(consultas), Assert.True);
    }

    [Fact]
    public async Task ReutilizaResolucionEntrePatentesYSinCompartirUsuarios()
    {
        var llamadas = 0;
        var contexto = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var permisos = new PermisosVista(id =>
        {
            llamadas++;
            return Task.FromResult(new PermisosVisuales(false, id == 1 ? new[] { "STOCK_VER" } : Array.Empty<string>()));
        }, contexto);
        for (var i = 0; i < 20; i++)
        {
            Assert.True(await permisos.TienePermisoAsync(1, "stock_ver"));
            Assert.False(await permisos.TienePermisoAsync(1, "USUARIO_CREAR"));
        }
        Assert.False(await permisos.TienePermisoAsync(2, "STOCK_VER"));
        Assert.Equal(2, llamadas);
    }

    [Fact]
    public async Task NuevaPeticionReflejaRevocacionInclusoReutilizandoInstancia()
    {
        var vigente = true;
        var contexto = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var permisos = new PermisosVista(_ => Task.FromResult(new PermisosVisuales(false,
            vigente ? new[] { "STOCK_VER" } : Array.Empty<string>())), contexto);
        Assert.True(await permisos.TienePermisoAsync(1, "STOCK_VER"));
        vigente = false;
        contexto.HttpContext = new DefaultHttpContext();
        Assert.False(await permisos.TienePermisoAsync(1, "STOCK_VER"));
    }

    [Fact]
    public async Task SinHttpNoConservaResultado_YAdminMantieneBypassVisual()
    {
        var admin = true;
        var permisos = new PermisosVista(_ => Task.FromResult(new PermisosVisuales(admin, Array.Empty<string>())), new HttpContextAccessor());
        Assert.True(await permisos.TienePermisoAsync(1, "CUALQUIERA"));
        admin = false;
        Assert.False(await permisos.TieneAlgunoAsync(1, "STOCK_VER", "USUARIO_VER"));
    }
}
