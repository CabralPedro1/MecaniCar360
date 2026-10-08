using System.Reflection;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Tests;

public class TurnoConsultasTests
{
    [Theory]
    [InlineData("todos", QueryTrackingBehavior.TrackAll)]
    [InlineData("detalle", QueryTrackingBehavior.TrackAll)]
    [InlineData("propios", QueryTrackingBehavior.TrackAll)]
    [InlineData("propio", QueryTrackingBehavior.TrackAll)]
    [InlineData("todos", QueryTrackingBehavior.NoTracking)]
    [InlineData("detalle", QueryTrackingBehavior.NoTracking)]
    [InlineData("propios", QueryTrackingBehavior.NoTracking)]
    [InlineData("propio", QueryTrackingBehavior.NoTracking)]
    public void ConsultaExtraida_ConservaExpresionYSqlAnterior(string caso, QueryTrackingBehavior tracking)
    {
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=SoloMetadatos;Integrated Security=true;Connect Timeout=1")
            .UseQueryTrackingBehavior(tracking).Options);
        var servicio = new TurnoService(db, null!, null!, null!, null!, null!);
        var metodo = typeof(TurnoService).GetMethod(
            caso == "propios" ? "ConsultarConVehiculo" : "ConsultarConDetalle",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(metodo);
        var actual = Assert.IsAssignableFrom<IQueryable<Turno>>(metodo.Invoke(servicio, null));

        // Referencia independiente: grafo utilizado antes de extraer las consultas.
        IQueryable<Turno> anterior = db.Turnos
            .Include(t => t.Vehiculo).ThenInclude(v => v!.Marca)
            .Include(t => t.Vehiculo).ThenInclude(v => v!.Modelo);
        if (caso != "propios")
            anterior = anterior.Include(t => t.Cliente)
                .Include(t => t.IngresoVehiculo).ThenInclude(i => i!.OrdenTrabajo);

        IQueryable<Turno> AplicarCondiciones(IQueryable<Turno> consulta) => caso switch
        {
            "todos" => consulta.OrderBy(t => t.FechaInicio),
            "detalle" => consulta.Where(t => t.Id == 17).Take(1),
            "propios" => consulta.Where(t => t.ClienteId == 23).OrderByDescending(t => t.FechaInicio),
            "propio" => consulta.Where(t => t.Id == 17 && t.ClienteId == 23).Take(1),
            _ => throw new InvalidOperationException()
        };
        anterior = AplicarCondiciones(anterior);
        actual = AplicarCondiciones(actual);
        Assert.Equal(anterior.Expression.ToString(), actual.Expression.ToString());
        Assert.Equal(anterior.ToQueryString(), actual.ToQueryString());
        Assert.Equal(tracking, db.ChangeTracker.QueryTrackingBehavior);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }
}
