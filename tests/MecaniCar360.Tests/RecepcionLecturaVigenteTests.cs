using Xunit;

namespace MecaniCar360.Tests;

// Regresiones estructurales sin base de datos. La materializacion con dos
// contextos y el rollback real requieren integracion SQL Server aislada.
public class RecepcionLecturaVigenteTests
{
    private static string Recepcion()
    {
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz != null && !File.Exists(Path.Combine(raiz.FullName, "MecaniCar360.csproj"))) raiz = raiz.Parent;
        Assert.NotNull(raiz);
        var fuente = File.ReadAllText(Path.Combine(raiz.FullName, "Services/IngresoVehiculoService.cs"));
        var inicio = fuente.IndexOf("RegistrarIngresoYCrearOrdenAsync(", StringComparison.Ordinal);
        var fin = fuente.IndexOf("ObtenerVehiculosEnTallerAsync(", inicio, StringComparison.Ordinal);
        Assert.True(inicio >= 0 && fin > inicio);
        return fuente[inicio..fin];
    }

    [Fact]
    public void RecargaDespuesDelBloqueoAntesDeValidarYEscribir()
    {
        var fuente = Recepcion();
        var posicion = 0;
        foreach (var paso in new[] { "BeginTransactionAsync(System.Data.IsolationLevel.Serializable)",
            "WITH (UPDLOCK, HOLDLOCK)", "ReloadAsync()", "EntityState.Detached",
            "if (turno.Estado == EstadoTurno.Cancelado)", "new IngresoVehiculo",
            "_context.IngresosVehiculo.Add(", "_context.TurnoEstados.Add(",
            "_context.OrdenesTrabajo.Add(", "_context.OrdenTrabajoEstados.Add(",
            "_auditoria.RegistrarOperacion(", "await transaction.CommitAsync();" })
        {
            var siguiente = fuente.IndexOf(paso, posicion, StringComparison.Ordinal);
            Assert.True(siguiente >= posicion, $"Paso ausente o fuera de orden: {paso}");
            posicion = siguiente + paso.Length;
        }
        Assert.Equal(1, fuente.Split("BeginTransactionAsync(", StringSplitOptions.None).Length - 1);
    }

    [Theory]
    [InlineData("turno == null || _context.Entry(turno).State == EntityState.Detached", "Turno no encontrado.")]
    [InlineData("turno.Estado == EstadoTurno.Cancelado", "No se puede registrar el ingreso de un turno cancelado.")]
    [InlineData("turno.Estado == EstadoTurno.ClienteAusente", "El cliente fue marcado como ausente.")]
    [InlineData("turno.Estado == EstadoTurno.Finalizado", "El turno ya fue atendido.")]
    public void RechazoConservaMensajeYPrecedeATodosLosRegistros(string condicion, string mensaje)
    {
        var fuente = Recepcion();
        var inicio = fuente.IndexOf("if (" + condicion + ")", StringComparison.Ordinal);
        Assert.True(inicio > fuente.IndexOf("ReloadAsync()", StringComparison.Ordinal));
        var fin = fuente.IndexOf('}', inicio);
        var rechazo = fuente[inicio..fin];
        Assert.Contains("return ServiceResult<OrdenTrabajo>.Error(", rechazo);
        Assert.Contains(mensaje, rechazo);
        Assert.True(fin < fuente.IndexOf("new IngresoVehiculo", StringComparison.Ordinal));
        Assert.DoesNotContain("SaveChanges", rechazo);
        Assert.DoesNotContain(".Add(", rechazo);
        Assert.DoesNotContain("RegistrarOperacion", rechazo);
    }

    [Fact]
    public void ConservaUnicamentePendienteYConfirmadoComoEstadosAdmitidos()
    {
        var fuente = Recepcion();
        var inicio = fuente.IndexOf("if (turno.Estado != EstadoTurno.Pendiente &&", StringComparison.Ordinal);
        Assert.True(inicio > fuente.IndexOf("ReloadAsync()", StringComparison.Ordinal));
        var fin = fuente.IndexOf('}', inicio);
        Assert.Contains("turno.Estado != EstadoTurno.Confirmado", fuente[inicio..fin]);
        Assert.Contains("Solo se puede registrar el ingreso de un turno pendiente o confirmado.", fuente[inicio..fin]);
        Assert.True(fin < fuente.IndexOf("new IngresoVehiculo", StringComparison.Ordinal));
    }
}
