using MecaniCar360.Models;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Concurrency;

public sealed partial class SqlConcurrencyTests
{
    // Reuses the guarded database, bounded race/drain and B1 limitation of the harness.
    [SqlConcurrencyTheory]
    [Trait("Category", "SqlConcurrency")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Diagnostico_InicioUnico(bool precargado)
    {
        var env = new ConcurrencyEnvironment();
        try
        {
            var f = await ConcurrencyFixtures.CreateAsync(env);
            var id = await f.OrdenAsync(env);
            await using (var setup = await env.OpenAsync(f.Admin))
                Assert.True((await setup.Ordenes.AsignarMecanicoAsync(id, f.MechanicPerson1, f.Admin)).Exitoso);
            if (precargado)
            {
                await using var stale = await env.OpenAsync(f.Mechanic1);
                var tracked = await stale.Db.OrdenesTrabajo.SingleAsync(o => o.Id == id);
                await using (var winner = await env.OpenAsync(f.Mechanic1))
                    Assert.True((await winner.Diagnosticos.IniciarAsync(id, f.Mechanic1)).Exitoso);
                var before = await ConcurrencyFixtures.SnapshotAsync(env);
                var rejected = await stale.Diagnosticos.IniciarAsync(id, f.Mechanic1);
                Assert.False(rejected.Exitoso);
                Assert.Equal("La orden debe estar pendiente y sin finalizar para iniciar el diagnóstico.", rejected.Mensaje);
                Assert.Equal(EstadoOrden.Diagnostico, tracked.EstadoActual);
                Assert.Equal(before, await ConcurrencyFixtures.SnapshotAsync(env));
            }
            else
            {
                var (a, b) = await Race(env, f.Mechanic1, f.Mechanic1, "OrdenesTrabajo",
                    async s => Outcome.From(await s.Diagnosticos.IniciarAsync(id, f.Mechanic1)),
                    async s => Outcome.From(await s.Diagnosticos.IniciarAsync(id, f.Mechanic1)));
                Assert.True(a.Success, a.Message);
                Assert.False(b.Success);
                Assert.Equal("La orden debe estar pendiente y sin finalizar para iniciar el diagnóstico.", b.Message);
            }
            await using var read = await env.OpenAsync();
            Assert.Equal(EstadoOrden.Diagnostico, (await read.Db.OrdenesTrabajo.SingleAsync(o => o.Id == id)).EstadoActual);
            Assert.Equal(1, await read.Db.OrdenTrabajoEstados.CountAsync(h => h.OrdenTrabajoId == id && h.Estado == EstadoOrden.Diagnostico));
            Assert.Equal(1, await read.Db.Auditorias.CountAsync(a => a.EntidadId == id && a.Entidad == "OrdenTrabajo" && a.Accion == "DIAGNOSTICO_INICIADO"));
        }
        finally { await env.VerifyAsync(); }
    }

    [SqlConcurrencyTheory]
    [Trait("Category", "SqlConcurrency")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reparacion_EntidadPrecargadaNoRepiteTransicion(bool finalizar)
    {
        var env = new ConcurrencyEnvironment();
        try
        {
            var f = await ConcurrencyFixtures.CreateAsync(env);
            var id = await f.OrdenAsync(env);
            // Arrange a valid approved budget on this fixture only, never on application data.
            await using (var setup = await env.OpenAsync(f.Admin))
            {
                var ot = await setup.Db.OrdenesTrabajo.SingleAsync(o => o.Id == id);
                ot.MecanicoId = f.MechanicPerson1;
                ot.EstadoActual = finalizar ? EstadoOrden.EnReparacion : EstadoOrden.Aprobado;
                setup.Db.Presupuestos.Add(new Presupuesto {
                    OrdenTrabajoId = id, MecanicoId = f.MechanicPerson1, Estado = EstadoPresupuesto.Aprobado,
                    FechaUltimaModificacion = DateTime.Now,
                    Versiones = new List<PresupuestoVersion> { new() {
                        NumeroVersion = 1, Decision = EstadoPresupuestoVersion.Aprobado,
                        EnviadaPorUsuarioId = f.Admin, FechaEnvio = DateTime.Now } }
                });
                await setup.Db.SaveChangesAsync();
            }
            await using var stale = await env.OpenAsync(f.Mechanic1);
            var tracked = await stale.Db.OrdenesTrabajo.SingleAsync(o => o.Id == id);
            await using (var winner = await env.OpenAsync(f.Mechanic1))
            {
                var ok = finalizar ? await winner.Ordenes.FinalizarAsync(id, f.Mechanic1, 2)
                    : await winner.Ordenes.IniciarReparacionAsync(id, f.Mechanic1);
                Assert.True(ok.Exitoso, ok.Mensaje);
            }
            var before = await ConcurrencyFixtures.SnapshotAsync(env);
            var rejected = finalizar ? await stale.Ordenes.FinalizarAsync(id, f.Mechanic1, 9)
                : await stale.Ordenes.IniciarReparacionAsync(id, f.Mechanic1);
            Assert.False(rejected.Exitoso);
            Assert.Equal(finalizar ? "La orden ya está finalizada." : "La orden debe estar aprobada para iniciar la reparación.", rejected.Mensaje);
            var expected = finalizar ? EstadoOrden.Finalizado : EstadoOrden.EnReparacion;
            Assert.Equal(expected, tracked.EstadoActual);
            // Independent context snapshot includes notifications, history, audit and DVH/DVV.
            Assert.Equal(before, await ConcurrencyFixtures.SnapshotAsync(env));
            await using var read = await env.OpenAsync();
            var persisted = await read.Db.OrdenesTrabajo.SingleAsync(o => o.Id == id);
            Assert.Equal(expected, persisted.EstadoActual);
            if (finalizar) Assert.Equal(2, persisted.HorasReales);
            Assert.Equal(1, await read.Db.OrdenTrabajoEstados.CountAsync(h => h.OrdenTrabajoId == id && h.Estado == expected));
        }
        finally { await env.VerifyAsync(); }
    }
}
