using Xunit;

namespace MecaniCar360.Tests;

// Comprobaciones estructurales; no sustituyen carreras ni rollback en SQL Server.
public class TurnoTransicionesAtomicidadTests
{
    private static string Metodo(string nombre)
    {
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz != null && !File.Exists(Path.Combine(raiz.FullName, "MecaniCar360.csproj"))) raiz = raiz.Parent;
        Assert.NotNull(raiz);
        var fuente = File.ReadAllText(Path.Combine(raiz.FullName, "Services/TurnoService.cs"));
        var inicio = fuente.IndexOf("public async Task<ServiceResult> " + nombre + "(", StringComparison.Ordinal);
        Assert.True(inicio >= 0);
        var fin = fuente.IndexOf("public async Task<ServiceResult>", inicio + 1, StringComparison.Ordinal);
        Assert.True(fin > inicio);
        return fuente[inicio..fin];
    }

    private static void EnOrden(string fuente, params string[] pasos)
    {
        var posicion = 0;
        foreach (var paso in pasos)
        {
            var indice = fuente.IndexOf(paso, posicion, StringComparison.Ordinal);
            Assert.True(indice >= posicion, $"Paso ausente o fuera de orden: {paso}");
            posicion = indice + paso.Length;
        }
    }

    [Theory]
    [InlineData("ConfirmarAsync", "TURNO_CONFIRMAR", "Pendiente", "Confirmado", "TURNO_CONFIRMADO")]
    [InlineData("MarcarClienteAusenteAsync", "TURNO_MODIFICAR", "Confirmado", "ClienteAusente", "CLIENTE_AUSENTE")]
    public void TransicionPermitida_LecturaFrescaYEscriturasDentroDeUnicaTransaccion(
        string metodo, string permiso, string origen, string destino, string evento)
    {
        var fuente = Metodo(metodo);
        EnOrden(fuente, "\"" + permiso + "\"", "await using var tx =",
            "BeginTransactionAsync(System.Data.IsolationLevel.Serializable)", "FromSqlInterpolated(",
            "WITH (UPDLOCK, HOLDLOCK)", "ReloadAsync()", "if (turno == null)",
            "if (turno.Estado != EstadoTurno." + origen + ")", "turno.Estado = EstadoTurno." + destino,
            "_context.TurnoEstados.Add(", "Estado = EstadoTurno." + destino,
            "_auditoria.RegistrarOperacion(\"" + evento + "\"", "await _context.SaveChangesAsync();",
            "await tx.CommitAsync();", "return ServiceResult.Ok(");
        foreach (var operacion in new[] { "BeginTransactionAsync(", "SaveChangesAsync()", "CommitAsync()", "TurnoEstados.Add(", "RegistrarOperacion(" })
            Assert.Equal(1, fuente.Split(operacion, StringSplitOptions.None).Length - 1);
    }

    [Theory]
    [InlineData("ConfirmarAsync", "Solo se pueden confirmar turnos pendientes.")]
    [InlineData("MarcarClienteAusenteAsync", "El turno debe estar confirmado.")]
    public void TransicionRechazada_ConservaMensajeYRetornaAntesDeTodaEscritura(string metodo, string mensaje)
    {
        var fuente = Metodo(metodo);
        var inicio = fuente.IndexOf("if (turno.Estado !=", StringComparison.Ordinal);
        var fin = fuente.IndexOf("turno.Estado =", inicio, StringComparison.Ordinal);
        var validacion = fuente[inicio..fin];
        EnOrden(validacion, "return ServiceResult.Error(", mensaje);
        Assert.DoesNotContain("TurnoEstados.Add", validacion);
        Assert.DoesNotContain("RegistrarOperacion", validacion);
        Assert.DoesNotContain("SaveChanges", validacion);
        Assert.DoesNotContain("CommitAsync", validacion);
    }

    [Fact]
    public void Ausente_ValidaFechaRecargadaAntesDeModificarEstado()
    {
        EnOrden(Metodo("MarcarClienteAusenteAsync"), "ReloadAsync()",
            "if (turno.FechaInicio > DateTime.Now)", "return ServiceResult.Error(",
            "No se puede marcar como ausente antes del horario del turno.",
            "turno.Estado = EstadoTurno.ClienteAusente");
    }
}
