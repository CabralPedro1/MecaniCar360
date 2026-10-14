using System.Security.Claims;
using MecaniCar360.Data;
using MecaniCar360.Services;
using MecaniCar360.Patterns.State;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MecaniCar360.Concurrency;

public sealed class SqlConcurrencyTheoryAttribute : TheoryAttribute
{
    public SqlConcurrencyTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("MC360_CONCURRENCY_EXECUTE") != "1")
            Skip = "Explicit isolated SQL execution authorization required.";
    }
}

public sealed class ConcurrencyEnvironment
{
    private static readonly SemaphoreSlim ClaimLock = new(1,1);
    private static string? claimedRun;
    public IsolatedDatabaseGuard Guard { get; } = IsolatedDatabaseGuard.FromEnvironment();
    public async Task<ActorContext> OpenAsync(int actor = 0, params IInterceptor[] interceptors)
    {
        if(File.Exists(Path.Combine(Path.GetTempPath(), Guard.Database, "ABORTED-NO-REUSE.txt")))
            throw new InvalidOperationException("Execution aborted previously; this isolated database cannot be reused.");
        var options = new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(Guard.ConnectionString)
            .AddInterceptors(interceptors).Options;
        var db = new MecaniCarContext(options);
        try
        {
            await db.Database.OpenConnectionAsync();
            await Guard.CheckRunnerAsync((SqlConnection)db.Database.GetDbConnection());
            if (db.Database.HasPendingModelChanges() || (await db.Database.GetPendingMigrationsAsync()).Any())
                throw new InvalidOperationException("Prepared schema does not match current code.");
            await ClaimLock.WaitAsync();
            try
            {
                if (claimedRun != null && claimedRun != Guard.RunId)
                    throw new InvalidOperationException("Cannot change run/database within a test process.");
                if (claimedRun == null)
                {
                    using var claim = db.Database.GetDbConnection().CreateCommand();
                    claim.CommandText = "INSERT dbo.MC360ConcurrencyExecution(RunId,StartedUtc) VALUES(@run,SYSUTCDATETIME())";
                    var value = claim.CreateParameter(); value.ParameterName="@run"; value.Value=Guard.RunId; claim.Parameters.Add(value);
                    // PK collision intentionally fails: no second process or rerun on this database.
                    await claim.ExecuteNonQueryAsync();
                    claimedRun = Guard.RunId;
                }
            }
            finally { ClaimLock.Release(); }
            return new ActorContext(db, actor);
        }
        catch { await db.DisposeAsync(); throw; }
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public void AbortUnfinishedParticipants()
    {
        // Do not open SQL or run final verification while a participant may still
        // be active. The existing persisted run claim also prohibits any rerun.
        try
        {
            var directory=Path.Combine(Path.GetTempPath(),Guard.Database);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory,"ABORTED-NO-REUSE.txt"),
                "Participant termination not confirmed. Do not reuse this isolated database.");
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine($"Unable to persist local abort marker ({ex.GetType().Name}); SQL run claim remains consumed.");
        }
        finally
        {
            Console.Error.WriteLine("FATAL: participants did not terminate. Aborting test host; isolated database must not be reused.");
            // Stops all scenarios, without running finally blocks or disposing a
            // DbContext concurrently with its active operation. No crash dump.
            Environment.Exit(2);
        }
        throw new InvalidOperationException("Process termination unexpectedly returned.");
    }

    public async Task VerifyAsync()
    {
        await using var scope = await OpenAsync();
        Assert.True((await new IntegridadService(scope.Db).VerificarAsync()).EsValida, "DVH/DVV must remain valid.");
        // Check every declared FK with read-only anti-joins (no DBCC/admin privilege).
        using var metadata = scope.Db.Database.GetDbConnection().CreateCommand();
        metadata.CommandText = """
          SELECT QUOTENAME(OBJECT_SCHEMA_NAME(f.parent_object_id))+'.'+QUOTENAME(OBJECT_NAME(f.parent_object_id)),
                 QUOTENAME(OBJECT_SCHEMA_NAME(f.referenced_object_id))+'.'+QUOTENAME(OBJECT_NAME(f.referenced_object_id)),
                 STRING_AGG(CONVERT(nvarchar(max),'p.'+QUOTENAME(pc.name)+' IS NOT NULL'),' AND '),
                 STRING_AGG(CONVERT(nvarchar(max),'p.'+QUOTENAME(pc.name)+'=r.'+QUOTENAME(rc.name)),' AND ')
          FROM sys.foreign_key_columns f
          JOIN sys.columns pc ON pc.object_id=f.parent_object_id AND pc.column_id=f.parent_column_id
          JOIN sys.columns rc ON rc.object_id=f.referenced_object_id AND rc.column_id=f.referenced_column_id
          GROUP BY f.constraint_object_id,f.parent_object_id,f.referenced_object_id
          """;
        var queries = new List<string>();
        using (var rows = await metadata.ExecuteReaderAsync())
            while (await rows.ReadAsync()) queries.Add($"SELECT COUNT_BIG(*) FROM {rows.GetString(0)} p WHERE {rows.GetString(2)} AND NOT EXISTS(SELECT 1 FROM {rows.GetString(1)} r WHERE {rows.GetString(3)})");
        foreach (var query in queries)
        {
            using var command = scope.Db.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;
            Assert.Equal(0L, Convert.ToInt64(await command.ExecuteScalarAsync()));
        }
    }
}

public sealed class ActorContext : IAsyncDisposable
{
    public MecaniCarContext Db { get; }
    private readonly ServiceProvider provider;
    public TurnoService Turnos => provider.GetRequiredService<TurnoService>();
    public OrdenTrabajoService Ordenes => provider.GetRequiredService<OrdenTrabajoService>();
    public IngresoVehiculoService Recepcion => provider.GetRequiredService<IngresoVehiculoService>();
    public ActorContext(MecaniCarContext db, int actor)
    {
        Db = db;
        // Fixed accessor per independent scope; no ambient shared identity.
        var accessor = new FixedAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, actor.ToString()) }, "IsolatedFixture")) } };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["ConfiguracionTaller:TurnosPorFranja"]="2", ["ConfiguracionTaller:DuracionFranjaTurnoMinutos"]="30",
            ["ConfiguracionTaller:HoraApertura"]="08:00", ["ConfiguracionTaller:HoraCierre"]="18:00"
        }).Build();
        provider = new ServiceCollection().AddLogging().AddSingleton(db).AddSingleton<IHttpContextAccessor>(accessor)
            .AddSingleton<IConfiguration>(config).AddTransient<PermisoService>().AddTransient<AuditoriaService>()
            .AddTransient<NotificacionService>().AddTransient<AgendaService>().AddTransient<VehiculoService>()
            .AddTransient<DominioVehicularService>().AddTransient<TurnoService>().AddTransient<OrdenTrabajoService>()
            .AddTransient<OrdenStateService>().AddTransient<DiagnosticoService>().AddTransient<IngresoVehiculoService>()
            .BuildServiceProvider();
    }
    public async ValueTask DisposeAsync() { await provider.DisposeAsync(); await Db.DisposeAsync(); }
    private sealed class FixedAccessor : IHttpContextAccessor { public HttpContext? HttpContext { get; set; } }
}
