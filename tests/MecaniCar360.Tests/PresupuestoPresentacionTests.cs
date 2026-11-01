using System.Reflection;
using MecaniCar360.Controllers;
using MecaniCar360.Models;
using MecaniCar360.Models.Enums;
using MecaniCar360.Services;
using Xunit;

namespace MecaniCar360.Tests;

public class PresupuestoPresentacionTests
{
    [Theory]
    [InlineData("ADMIN", 9, false)]
    [InlineData("CAJA", 9, false)]
    [InlineData("MECANICO", 9, false)]
    [InlineData("CLIENTE", 10, false)]
    [InlineData("CLIENTE", 9, true)]
    public void ActorPropietario_FiltroRealNoConcedeDecisionAInternos(string rol, int personaId, bool esperado)
    {
        var usuario = new Usuario { Id = 1, PersonaId = personaId, Username = "fixture",
            EmailLogin = "fixture@example.invalid", Persona = new Persona { Id = personaId,
                Roles = new() { new() { Rol = new Rol { Nombre = rol, Activo = true } } } } };
        var filtro = typeof(PresupuestoService).GetMethod("ClientesParaDecision", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(filtro);
        var candidatos = Assert.IsAssignableFrom<IQueryable<Usuario>>(filtro.Invoke(null,
            new object[] { new[] { usuario }.AsQueryable(), 9 }));
        Assert.Equal(esperado, candidatos.Any());
        // El resto de la habilitación (correo, credenciales y onboarding) lo sigue
        // resolviendo ClienteHabilitadoService; no se sustituye por este filtro.
    }

    [Theory]
    [InlineData(false, "vigente", false)]
    [InlineData(true, "vigente", true)]
    [InlineData(true, "decidida", false)]
    [InlineData(true, "borrador", false)]
    [InlineData(true, "reparacion", false)]
    [InlineData(true, "finalizada", false)]
    [InlineData(true, "facturada", false)]
    public void Disponibilidad_ExigeAutorizacionDelActorYVersionElegible(bool autorizado, string estado, bool esperado)
    {
        var orden = new OrdenTrabajo { EstadoActual = EstadoOrden.EsperandoAprobacion };
        var presupuesto = new Presupuesto { Estado = EstadoPresupuesto.Pendiente, OrdenTrabajo = orden };
        var version = new PresupuestoVersion { Decision = EstadoPresupuestoVersion.Pendiente, Presupuesto = presupuesto };
        if (estado == "decidida") version.Decision = EstadoPresupuestoVersion.Aprobado;
        if (estado == "borrador") presupuesto.Estado = EstadoPresupuesto.Modificado;
        if (estado == "reparacion") orden.EstadoActual = EstadoOrden.EnReparacion;
        if (estado == "finalizada") orden.FechaFin = DateTime.Today;
        if (estado == "facturada") orden.Factura = new Factura();
        var metodo = typeof(PresupuestoController).GetMethod("PuedeDecidir", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(metodo);
        Assert.Equal(esperado, Assert.IsType<bool>(metodo.Invoke(null, new object[] { version, autorizado })));
    }

    [Fact]
    public void HtmlYJson_UsanAutorizacionCompartida_SinModificarPostNiConsulta()
    {
        // Evidencia estructural complementaria: no simula una ejecución HTTP/SQL.
        var controller = Fuente("Controllers/PresupuestoController.cs");
        Assert.Equal(2, controller.Split("await _service.PuedeDecidirAsync(resultado.Data!.Id, usuario)").Length - 1);
        var service = Fuente("Services/PresupuestoService.cs");
        var inicio = service.IndexOf("public async Task<bool> PuedeDecidirAsync", StringComparison.Ordinal);
        var fin = service.IndexOf("private async Task<bool> ClienteDecisorAsync", inicio, StringComparison.Ordinal);
        var consulta = service[inicio..fin];
        Assert.Contains("ClienteDecisorAsync(usuario, versionId)", consulta);
        Assert.Contains("CLIENTE_PRESUPUESTO_APROBAR", consulta);
        Assert.Contains("CLIENTE_PRESUPUESTO_RECHAZAR", consulta);
        Assert.DoesNotContain("SaveChanges", consulta);
        Assert.DoesNotContain("BeginTransaction", consulta);
        var vista = Fuente("Views/Presupuesto/Propio.cshtml");
        var acciones = vista[vista.IndexOf("@if (vigente)", StringComparison.Ordinal)..];
        Assert.Contains("asp-action=\"Aprobar\" method=\"post\"", acciones);
        Assert.Contains("asp-action=\"Rechazar\" method=\"post\"", acciones);
        Assert.Equal(2, acciones.Split("@Html.AntiForgeryToken()").Length - 1);
        Assert.Contains("name=\"presupuestoVersionId\"", acciones);
    }

    private static string Fuente(string archivo)
    {
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz != null && !File.Exists(Path.Combine(raiz.FullName, "MecaniCar360.csproj"))) raiz = raiz.Parent;
        Assert.NotNull(raiz);
        return File.ReadAllText(Path.Combine(raiz.FullName, archivo));
    }
}
