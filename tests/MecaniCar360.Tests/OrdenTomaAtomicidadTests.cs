using Xunit;

namespace MecaniCar360.Tests;

// Regresiones estructurales sin conexion ni escrituras. Las carreras reales
// requieren SQL Server aislado con el interceptor DVH/DVV habilitado.
public class OrdenTomaAtomicidadTests
{
    private static string Fuente(string archivo)
    {
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz != null && !File.Exists(Path.Combine(raiz.FullName, "MecaniCar360.csproj")))
            raiz = raiz.Parent;
        Assert.NotNull(raiz);
        return File.ReadAllText(Path.Combine(raiz.FullName, archivo));
    }

    private static string Metodo(string nombre, string siguiente)
    {
        var fuente = Fuente("Services/OrdenTrabajoService.cs");
        var inicio = fuente.IndexOf(nombre + "(", StringComparison.Ordinal);
        Assert.True(inicio >= 0);
        var fin = fuente.IndexOf(siguiente, inicio + nombre.Length, StringComparison.Ordinal);
        Assert.True(fin > inicio);
        return fuente[inicio..fin];
    }

    private static string Toma() => Metodo("TomarOrdenAsync", "Orden tomada correctamente.");

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

    [Fact]
    public void TomaLibre_ConservaEstadoPermisoYEscrituraUnicaDentroDeTransaccion()
    {
        var toma = Toma();
        EnOrden(toma, "\"ORDEN_MODIFICAR\"", "EsMecanicoActivoAsync(",
            "await using var tx =", "BeginTransactionAsync(System.Data.IsolationLevel.Serializable)",
            "FromSqlInterpolated(", "WITH (UPDLOCK, HOLDLOCK)", "ReloadAsync()",
            "if (orden == null)", "if (orden.FechaFin.HasValue)", "if (orden.MecanicoId.HasValue)",
            "EstadoOrden.Pendiente", "orden.MecanicoId =", "_auditoria.RegistrarOperacion(\"MECANICO_ASIGNADO\"",
            "await _context.SaveChangesAsync();", "await tx.CommitAsync();", "return ServiceResult.Ok(");
        Assert.Equal(1, toma.Split("SaveChangesAsync()", StringSplitOptions.None).Length - 1);
        Assert.Equal(1, toma.Split("RegistrarOperacion(", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void TomaOcupada_RetornaAntesDeAsignarAuditarOConfirmar()
    {
        var toma = Toma();
        var inicio = toma.IndexOf("if (orden.MecanicoId.HasValue)", StringComparison.Ordinal);
        var fin = toma.IndexOf("if (orden.EstadoActual", inicio, StringComparison.Ordinal);
        var ocupada = toma[inicio..fin];
        EnOrden(ocupada, "orden.MecanicoId.Value ==", "mecanicoId)", "return ServiceResult.Ok(",
            "La orden ya está asignada a este mecánico.", "return ServiceResult.Error(",
            "La orden ya fue tomada por otro mecánico.");
        Assert.DoesNotContain("SaveChanges", ocupada);
        Assert.DoesNotContain("RegistrarOperacion", ocupada);
        Assert.DoesNotContain("CommitAsync", ocupada);
        Assert.DoesNotContain("orden.MecanicoId =", ocupada);
    }

    [Fact]
    public void AsignacionYToma_ConservanOrdenDeBloqueosYPermisosDiferentes()
    {
        var asignacion = Metodo("AsignarMecanicoAsync", "TomarOrdenAsync(");
        Assert.Contains("\"ORDEN_ASIGNAR_MECANICO\"", asignacion);
        Assert.DoesNotContain("\"ORDEN_ASIGNAR_MECANICO\"", Toma());
        foreach (var metodo in new[] { asignacion, Toma() })
            EnOrden(metodo, "BeginTransactionAsync(System.Data.IsolationLevel.Serializable)",
                "FromSqlInterpolated(", "WITH (UPDLOCK, HOLDLOCK)", "ReloadAsync()",
                "orden.MecanicoId.HasValue", "SaveChangesAsync()", "CommitAsync()");
    }

    [Fact]
    public void InicioTransaccion_EsperaMutexExclusivoAntesDeDevolverControl()
    {
        Assert.Contains("AddInterceptors(Integridad.IntegridadTransactionInterceptor.Instancia)",
            Fuente("Data/MecaniCarContext.cs"));
        var interceptor = Fuente("Data/Integridad/IntegridadTransactionInterceptor.cs");
        Assert.Contains("@LockMode='Exclusive', @LockOwner='Transaction'", interceptor);
        EnOrden(interceptor[interceptor.IndexOf("ValueTask<DbTransaction> TransactionStartedAsync", StringComparison.Ordinal)..],
            "await TomarAsync(result, cancellationToken)", "return result;");
    }
}
