using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Concurrency;

public sealed partial class SqlConcurrencyTests
{
    private sealed record Outcome(bool Success,string Message)
    {
        public static Outcome From(ServiceResult r)=>new(r.Exitoso,r.Mensaje);
    }

    private static async Task<(Outcome first,Outcome second)> Race(
        ConcurrencyEnvironment env, int actor1,int actor2,string table,
        Func<ActorContext,Task<Outcome>> first, Func<ActorContext,Task<Outcome>> second)
    {
        using var stop=new CancellationTokenSource();
        var gate=new ControlledReadGate(table,stop.Token);
        var attempt=new TransactionAttempt();
        await using var a=await env.OpenAsync(actor1,gate,new CancellationBoundary(stop.Token));
        await using var b=await env.OpenAsync(actor2,attempt,new CancellationBoundary(stop.Token));
        Assert.NotSame(a.Db,b.Db);
        Assert.NotSame(a.Db.Database.GetDbConnection(),b.Db.Database.GetDbConnection());
        var task1=first(a);
        Task<Outcome>? task2=null;
        try
        {
            await Task.WhenAny(gate.Reached.Task,task1).WaitAsync(TimeSpan.FromSeconds(5));
            if(task1.IsCompleted) { await task1; throw new InvalidOperationException("Winner did not reach protected read."); }
            await gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(5));
            task2=second(b);
            await Task.WhenAny(attempt.Reached.Task,task2).WaitAsync(TimeSpan.FromSeconds(5));
            if(task2.IsCompleted) { await task2; throw new InvalidOperationException("Competitor did not attempt transaction."); }
            await attempt.Reached.Task.WaitAsync(TimeSpan.FromSeconds(5));
            gate.Release.TrySetResult();
            // This proves coordinated calls, NOT observed SQL lock contention.
            var results=await Task.WhenAll(task1,task2).WaitAsync(TimeSpan.FromSeconds(30));
            return (results[0],results[1]);
        }
        catch(Exception original)
        {
            stop.Cancel();
            gate.Release.TrySetResult();
            var participants=task2 == null ? new[] { task1 } : new[] { task1,task2 };
            // Observe completion without swallowing participant exceptions. Their
            // original exceptions are collected explicitly below once terminal.
            var terminal=Task.WhenAll(participants.Select(t=>t.ContinueWith(_=>{},
                CancellationToken.None,TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default)));
            if(await Task.WhenAny(terminal,Task.Delay(TimeSpan.FromSeconds(30))) != terminal)
                env.AbortUnfinishedParticipants();
            await terminal;
            var failures=new List<Exception> { original };
            foreach(var task in participants)
            {
                if(task.IsFaulted) failures.AddRange(task.Exception!.Flatten().InnerExceptions);
                else if(task.IsCanceled) failures.Add(new TaskCanceledException("Participant cancelled during failure drain."));
            }
            throw new AggregateException("Concurrent operation failed; all participants are terminal.",failures.Distinct());
        }
        finally { gate.Release.TrySetResult(); }
    }

    [SqlConcurrencyTheory]
    [Trait("Category","SqlConcurrency")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reservas_UltimoCupoOMismoVehiculo(bool mismoVehiculo)
    {
        var env=new ConcurrencyEnvironment();
        try
        {
            var f=await ConcurrencyFixtures.CreateAsync(env);
            if(!mismoVehiculo)
            {
                await using var setup=await env.OpenAsync(f.Admin);
                var result=await setup.Turnos.CrearAsync(f.Client,f.Vehicles[2],TipoTurno.Diagnostico,f.Slot,f.Label,f.Admin);
                Assert.True(result.Exitoso,result.Mensaje);
            }
            var (a,b)=await Race(env,f.Admin,f.Admin,"Turnos",
                async s=>Outcome.From(await s.Turnos.CrearAsync(f.Client,f.Vehicles[0],TipoTurno.Diagnostico,f.Slot,f.Label,f.Admin)),
                async s=>Outcome.From(await s.Turnos.CrearAsync(f.Client,f.Vehicles[mismoVehiculo?0:1],TipoTurno.Diagnostico,
                    mismoVehiculo?f.Slot.AddMinutes(30):f.Slot,f.Label,f.Admin)));
            Assert.True(a.Success,a.Message); Assert.False(b.Success);
            Assert.Equal(mismoVehiculo?"El vehículo ya posee un turno activo.":"No hay disponibilidad para ese horario.",b.Message);
            await using var read=await env.OpenAsync();
            var turns=await read.Db.Turnos.Where(t=>t.ClienteId==f.Client).ToListAsync();
            Assert.Equal(mismoVehiculo?1:2,turns.Count);
            var ids=turns.Select(t=>t.Id).ToArray();
            Assert.Equal(turns.Count,await read.Db.TurnoEstados.CountAsync(h=>ids.Contains(h.TurnoId)));
            Assert.Equal(turns.Count,await read.Db.Auditorias.CountAsync(x=>x.UsuarioId==f.Admin && x.Accion=="TURNO_CREADO"));
        }
        finally { await env.VerifyAsync(); }
    }

    [SqlConcurrencyTheory]
    [Trait("Category","SqlConcurrency")]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task Toma_ContraTomaOAsignacion(bool administrativa,bool reverse)
    {
        var env=new ConcurrencyEnvironment();
        try
        {
            var f=await ConcurrencyFixtures.CreateAsync(env);
            var ot=await f.OrdenAsync(env);
            Func<ActorContext,Task<Outcome>> take=async s=>Outcome.From(await s.Ordenes.TomarOrdenAsync(ot,f.Mechanic1));
            Func<ActorContext,Task<Outcome>> other=async s=>Outcome.From(administrativa
                ? await s.Ordenes.AsignarMecanicoAsync(ot,f.MechanicPerson2,f.Admin)
                : await s.Ordenes.TomarOrdenAsync(ot,f.Mechanic2));
            var otherActor=administrativa?f.Admin:f.Mechanic2;
            var (a,b)=await Race(env,reverse?otherActor:f.Mechanic1,reverse?f.Mechanic1:otherActor,"OrdenesTrabajo",
                reverse?other:take,reverse?take:other);
            Assert.True(a.Success,a.Message); Assert.False(b.Success);
            Assert.Equal(!reverse && administrativa?"La orden ya está asignada a otro mecánico.":"La orden ya fue tomada por otro mecánico.",b.Message);
            await using var read=await env.OpenAsync();
            var order=await read.Db.OrdenesTrabajo.SingleAsync(o=>o.Id==ot);
            Assert.Equal(reverse?f.MechanicPerson2:f.MechanicPerson1,order.MecanicoId);
            Assert.Equal(EstadoOrden.Pendiente,order.EstadoActual);
            Assert.Equal(1,await read.Db.Auditorias.CountAsync(a=>a.Entidad=="OrdenTrabajo" && a.EntidadId==ot && a.Accion=="MECANICO_ASIGNADO"));
        }
        finally { await env.VerifyAsync(); }
    }

    [SqlConcurrencyTheory]
    [Trait("Category","SqlConcurrency")]
    [InlineData("confirmar","cancelar",false)] [InlineData("confirmar","cancelar",true)]
    [InlineData("confirmar","recepcion",false)] [InlineData("confirmar","recepcion",true)]
    [InlineData("ausente","recepcion",false)] [InlineData("ausente","recepcion",true)]
    [InlineData("ausente","cancelar",false)] [InlineData("ausente","cancelar",true)]
    [InlineData("ausente","reprogramar",false)] [InlineData("ausente","reprogramar",true)]
    public async Task Transiciones_SerializanHistorial(string operation,string rival,bool reverse)
    {
        var env=new ConcurrencyEnvironment();
        try
        {
            var f=await ConcurrencyFixtures.CreateAsync(env);
            var turno=await f.TurnoAsync(env,operation=="confirmar"?EstadoTurno.Pendiente:EstadoTurno.Confirmado,past:operation=="ausente");
            var first=reverse?rival:operation; var second=reverse?operation:rival;
            var (a,b)=await Race(env,f.Admin,f.Admin,"Turnos",s=>Act(s,first,f,turno),s=>Act(s,second,f,turno));
            Assert.True(a.Success,a.Message);
            var both=first=="confirmar";
            Assert.Equal(both,b.Success);
            if(!both)
            {
                var expected=second switch {
                    "confirmar"=>"Solo se pueden confirmar turnos pendientes.",
                    "ausente" when first=="reprogramar"=>"No se puede marcar como ausente antes del horario del turno.",
                    "ausente"=>"El turno debe estar confirmado.",
                    "recepcion"=>"El cliente fue marcado como ausente.",
                    "cancelar"=>"No se puede cancelar un turno marcado como cliente ausente.",
                    "reprogramar"=>"Solo se pueden reprogramar turnos pendientes o confirmados.",
                    _=>throw new InvalidOperationException()
                };
                Assert.Equal(expected,b.Message);
            }
            var finalOperation=both?second:first;
            var expectedState=finalOperation switch { "recepcion"=>EstadoTurno.Finalizado,"cancelar"=>EstadoTurno.Cancelado,
                "ausente"=>EstadoTurno.ClienteAusente,_=>EstadoTurno.Confirmado };
            await using var read=await env.OpenAsync();
            var persisted=await read.Db.Turnos.SingleAsync(t=>t.Id==turno);
            Assert.Equal(expectedState,persisted.Estado);
            if(finalOperation=="reprogramar") Assert.Equal(f.Slot,persisted.FechaInicio);
            var history=await read.Db.TurnoEstados.Where(h=>h.TurnoId==turno).OrderBy(h=>h.Id).ToListAsync();
            Assert.Equal(both?3:2,history.Count); Assert.Equal(expectedState,history.Last().Estado);
            var count=finalOperation=="recepcion"?1:0;
            Assert.Equal(count,await read.Db.IngresosVehiculo.CountAsync(i=>i.TurnoId==turno));
            Assert.Equal(count,await read.Db.OrdenesTrabajo.CountAsync(o=>o.IngresoVehiculo.TurnoId==turno));
            Assert.Equal(both?2:1,await read.Db.Auditorias.CountAsync(x=>x.UsuarioId==f.Admin));
            string Audit(string op) => op switch { "confirmar"=>"TURNO_CONFIRMADO", "cancelar"=>"TURNO_CANCELADO",
                "ausente"=>"CLIENTE_AUSENTE", "reprogramar"=>"TURNO_REPROGRAMADO", "recepcion"=>"INGRESO_VEHICULO_ORDEN_CREADA", _=>throw new InvalidOperationException() };
            var audits=await read.Db.Auditorias.Where(x=>x.UsuarioId==f.Admin).OrderBy(x=>x.Id).Select(x=>x.Accion).ToArrayAsync();
            Assert.Equal(both?new[]{Audit(first),Audit(second)}:new[]{Audit(first)},audits);
        }
        finally { await env.VerifyAsync(); }
    }

    private static async Task<Outcome> Act(ActorContext s,string operation,ConcurrencyFixtures f,int turno)
    {
        if(operation=="recepcion")
        {
            var r=await s.Recepcion.RegistrarIngresoYCrearOrdenAsync(f.Reception(turno),f.Admin);
            return new(r.Exitoso,r.Mensaje);
        }
        return Outcome.From(await (operation switch {
            "confirmar"=>s.Turnos.ConfirmarAsync(turno,f.Admin), "cancelar"=>s.Turnos.CancelarAsync(turno,f.Admin),
            "ausente"=>s.Turnos.MarcarClienteAusenteAsync(turno,f.Admin),
            "reprogramar"=>s.Turnos.ReprogramarAsync(turno,f.Slot,f.Admin),
            _=>throw new InvalidOperationException()
        }));
    }

    [SqlConcurrencyTheory]
    [Trait("Category","SqlConcurrency")]
    [InlineData("cancelar")] [InlineData("ausente")]
    public async Task Recepcion_NoUsaTurnoPrecargadoObsoleto(string competing)
    {
        var env=new ConcurrencyEnvironment();
        try
        {
            var f=await ConcurrencyFixtures.CreateAsync(env);
            var id=await f.TurnoAsync(env,EstadoTurno.Confirmado,true);
            await using var stale=await env.OpenAsync(f.Admin);
            var tracked=await stale.Db.Turnos.SingleAsync(t=>t.Id==id);
            await using(var other=await env.OpenAsync(f.Admin)) Assert.True((await Act(other,competing,f,id)).Success);
            Assert.Equal(EstadoTurno.Confirmado,tracked.Estado);
            var before=await ConcurrencyFixtures.SnapshotAsync(env);
            var result=await Act(stale,"recepcion",f,id);
            Assert.False(result.Success);
            Assert.Equal(competing=="cancelar"?"No se puede registrar el ingreso de un turno cancelado.":"El cliente fue marcado como ausente.",result.Message);
            var after=await ConcurrencyFixtures.SnapshotAsync(env);
            Assert.True(before.SequenceEqual(after),"Rejected reception must leave all persisted rows unchanged.");
        }
        finally { await env.VerifyAsync(); }
    }

    [SqlConcurrencyTheory]
    [Trait("Category","SqlConcurrency")]
    [InlineData("alta",false)] [InlineData("toma",false)] [InlineData("confirmar",false)] [InlineData("ausente",false)] [InlineData("recepcion",false)]
    [InlineData("alta",true)] [InlineData("toma",true)] [InlineData("confirmar",true)] [InlineData("ausente",true)] [InlineData("recepcion",true)]
    public async Task FalloDespuesDeDml_RevierteTodo(string operation,bool beforeCommit)
    {
        var env=new ConcurrencyEnvironment();
        try
        {
            var f=await ConcurrencyFixtures.CreateAsync(env);
            var id=operation=="toma"?await f.OrdenAsync(env):operation=="alta"?0:
                await f.TurnoAsync(env,operation=="confirmar"?EstadoTurno.Pendiente:EstadoTurno.Confirmado,true);
            var before=await ConcurrencyFixtures.SnapshotAsync(env);
            var evidence=new WriteEvidence();
            var failure=new FailAfterWrite(evidence);
            var commitFailure=new FailBeforeCommit(evidence);
            await using(var actor=await env.OpenAsync(operation=="toma"?f.Mechanic1:f.Admin,
                evidence, beforeCommit?commitFailure:failure))
            {
                await Assert.ThrowsAsync<InjectedPersistenceFailure>(async ()=> {
                    if(operation=="alta") await actor.Turnos.CrearAsync(f.Client,f.Vehicles[0],TipoTurno.Diagnostico,f.Slot,f.Label,f.Admin);
                    else if(operation=="toma") await actor.Ordenes.TomarOrdenAsync(id,f.Mechanic1);
                    else await Act(actor,operation,f,id);
                });
            }
            Assert.True(beforeCommit?commitFailure.Fired:failure.Fired,"Failure must occur at the requested persistence boundary.");
            Assert.True(evidence.Written,"Rollback requires actual persisted DML inside the transaction.");
            if(beforeCommit) Assert.True(evidence.IntegrityMaintained,"Precommit failure requires changed and valid DVH/DVV.");
            var after=await ConcurrencyFixtures.SnapshotAsync(env);
            Assert.True(before.SequenceEqual(after),"Rollback must preserve all rows, histories, audit and DVH/DVV.");
        }
        finally { await env.VerifyAsync(); }
    }
}
