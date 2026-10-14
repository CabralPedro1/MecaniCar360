using System.Reflection;
using MecaniCar360.Data;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Tests;

public class TurnoAltaAtomicidadTests
{
    [Theory]
    [InlineData(0, "Debe indicar el motivo del turno.")]
    [InlineData(1, "Debe indicar una fecha para el turno programado.")]
    [InlineData(2, "Seleccione el vehículo previsto para el turno programado.")]
    [InlineData(3, "La fecha del turno debe ser futura.")]
    public async Task ValidacionesPrevias_ConservanMensajesYSinEscrituras(int caso, string mensaje)
    {
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=SoloMetadatos;Integrated Security=true;Connect Timeout=1").Options);
        var servicio = new TurnoService(db, null!, null!, null!, null!, null!);
        var metodo = typeof(TurnoService).GetMethod("CrearInternoAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var fecha = caso == 1 ? (DateTime?)null : caso == 3 ? DateTime.Now.AddDays(-1) : DateTime.Now.AddDays(1);
        var resultado = await (Task<ServiceResult>)metodo.Invoke(servicio,
            new object?[] { 1, caso == 2 ? (int?)null : 1, TipoTurno.Diagnostico, fecha,
                caso == 0 ? " " : "Prueba", 1, null, false })!;
        Assert.False(resultado.Exitoso);
        Assert.Equal(mensaje, resultado.Mensaje);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Fact]
    public void Alta_ConservaValidacionesYAmbasEscriturasDentroDelAmbitoTransaccional()
    {
        // Regresion estructural: no sustituye las carreras y rollback reales en SQL Server.
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz != null && !File.Exists(Path.Combine(raiz.FullName, "MecaniCar360.csproj"))) raiz = raiz.Parent;
        Assert.NotNull(raiz);
        var fuente = File.ReadAllText(Path.Combine(raiz.FullName, "Services", "TurnoService.cs"));
        var inicio = fuente.IndexOf("private async Task<ServiceResult> CrearInternoAsync", StringComparison.Ordinal);
        var fin = fuente.IndexOf("public async Task<ServiceResult> ConfirmarAsync", inicio, StringComparison.Ordinal);
        var alta = fuente[inicio..fin];
        var pasos = new[] {
            "await using var transaction = await _context.Database.BeginTransactionAsync();",
            "var tieneTurnoActivo =", "if (tieneTurnoActivo)",
            "if (!atencionInmediata)", "_agendaService.ValidarDisponibilidadAsync(fechaEfectiva)",
            "if (!disponibilidad.Exitoso) return disponibilidad;", "_context.Turnos.Add(turno);",
            "await _context.SaveChangesAsync();", "_context.TurnoEstados.Add(",
            "_auditoria.RegistrarOperacion(\"TURNO_CREADO\"", "await _context.SaveChangesAsync();",
            "await transaction.CommitAsync();"
        };
        var posicion = 0;
        foreach (var paso in pasos)
        {
            var siguiente = alta.IndexOf(paso, posicion, StringComparison.Ordinal);
            Assert.True(siguiente >= posicion, $"Paso fuera de orden o ausente: {paso}");
            posicion = siguiente + paso.Length;
        }
        Assert.Equal(1, alta.Split("BeginTransactionAsync()", StringSplitOptions.None).Length - 1);
        Assert.Equal(1, alta.Split("CommitAsync()", StringSplitOptions.None).Length - 1);
    }
}
