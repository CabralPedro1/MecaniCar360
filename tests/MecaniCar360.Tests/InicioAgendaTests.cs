using System.Reflection;
using MecaniCar360.Controllers;
using Xunit;

namespace MecaniCar360.Tests;

public class InicioAgendaTests
{
    [Theory]
    [InlineData(true, "TURNO_VER,DASHBOARD_VER", "Agenda", "Turno")]
    [InlineData(false, "TURNO_VER", "Agenda", "Turno")]
    [InlineData(false, "ORDEN_MODIFICAR", "Mecanico", "Inicio")]
    [InlineData(false, "STOCK_VER", "Stock", "Inicio")]
    [InlineData(false, "CLIENTE_ORDEN_VER", "Index", "PortalCliente")]
    [InlineData(false, "", "Index", "Home")]
    [InlineData(false, "DASHBOARD_VER", "Index", "Dashboard")]
    public async Task ResolucionRespetaPermisosYConservaOtrosDestinos(
        bool admin, string patentes, string accion, string controlador)
    {
        var disponibles = patentes.Split(',');
        Func<string[], Task<bool>> tiene = requeridos =>
            Task.FromResult(requeridos.Any(disponibles.Contains));
        var metodo = typeof(AccountController).GetMethod("ResolverDestinoAsync",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var resultado = await (Task<(string Accion, string Controlador)>)metodo.Invoke(null, new object[] { admin, tiene })!;
        Assert.Equal((accion, controlador), resultado);
    }

    [Fact]
    public void DashboardConservaPermisoYNavegacion_PrimerIngresoConservaProteccion()
    {
        var layout = CalificacionEntregaTests.Fuente("Views/Shared/_Layout.cshtml");
        Assert.Contains("DASHBOARD_VER", layout);
        Assert.Contains("asp-controller=" + '\"' + "Dashboard" + '\"', layout);
        Assert.Contains("DASHBOARD_VER", CalificacionEntregaTests.Fuente("Controllers/DashboardController.cs"));
        var account = CalificacionEntregaTests.Fuente("Controllers/AccountController.cs");
        Assert.Contains("DestinoInicialAsync(resultado.Usuario.Id)", account);
        Assert.Contains("DestinoInicialAsync(usuarioId)", account);
        Assert.Contains("PrimerLogin", CalificacionEntregaTests.Fuente("Middleware/PrimerLoginMiddleware.cs"));
    }
}
