using System.Reflection;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MecaniCar360.Concurrency;

public sealed partial class SqlConcurrencyTests
{
    private sealed class AdminClock(DateTime instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(instant, TimeSpan.Zero);
    }

    // Reflect only pure marker/hash helpers, never the privileged authorization capability.
    private static object AdminHelper(string method, params object[] args) =>
        typeof(Program).Assembly.GetType("MecaniCar360.Helpers.ActivacionAdminInicial")!
            .GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args)!;

    private sealed record AdminFixture(int Id, string Email, string Token, string Stamp, DateTime Created)
    {
        public ActivacionAdminService Service(ActorContext scope, DateTime? now = null) => new(scope.Db,
            new CorreoCuentaSimulado(), new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["BootstrapAdmin:Email"] = Email }).Build(),
            new AdminClock(now ?? Created), NullLogger<ActivacionAdminService>.Instance);
        public ActivarAdminViewModel Form(string? token = null) => new()
        {
            Token = token ?? Token, Password = "Fixture!9a-secure", ConfirmarPassword = "Fixture!9a-secure"
        };
    }

    private static async Task<AdminFixture> ArrangeAdmin(ConcurrencyEnvironment env)
    {
        await using var scope = await env.OpenAsync();
        var db = scope.Db;
        var label = "admin-" + Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var u = new Usuario { Username = label, EmailLogin = label + "@example.invalid",
            Activo = false, PrimerLogin = true, PasswordHash = null,
            Persona = new Persona { Nombre = "Fixture", Apellido = label, Email = label + "@example.invalid", Activo = true } };
        u.Persona.Roles.Add(new PersonaRol { Rol = await db.Roles.SingleAsync(r => r.Nombre == "ADMIN" && r.Activo) });
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        var link = (EnlacePasswordCliente)AdminHelper("CrearMarca", u, now);
        // Arrange an issued token. This is NOT a call to local issuance/reissuance.
        var token = TokenInvitacion.Generar();
        link.TokenHash = (string)AdminHelper("HashToken", token);
        link.FechaInvalidacion = null;
        db.EnlacesPasswordCliente.Add(link);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new(u.Id, u.EmailLogin, token, u.SecurityStamp, now);
    }

    private static async Task VerifyActivated(ConcurrencyEnvironment env, AdminFixture f)
    {
        await using var read = await env.OpenAsync();
        var u = await read.Db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == f.Id);
        var e = await read.Db.EnlacesPasswordCliente.AsNoTracking().SingleAsync(e => e.UsuarioId == f.Id);
        Assert.True(u.Activo); Assert.False(u.PrimerLogin);
        Assert.True(u.SecurityStamp != f.Stamp, "Activation must rotate the security stamp.");
        Assert.True(u.PasswordHash != null && BCrypt.Net.BCrypt.Verify(f.Form().Password, u.PasswordHash), "Persisted credential mismatch.");
        Assert.NotNull(e.FechaConsumida);
        Assert.Equal(1, await read.Db.Auditorias.CountAsync(a => a.Entidad == "Usuario" && a.EntidadId == f.Id &&
            a.Accion == "ADMIN_ACTIVADO" && a.UsuarioId == null));
        Assert.False(await f.Service(read).ValidarAsync(f.Token));
        await env.VerifyAsync();
    }

    [SqlConcurrencyTheory, Trait("Category", "SqlConcurrency"), Trait("Security", "ADMINACT")]
    [InlineData("ActivarYRepetir")]
    [InlineData("Concurrente")]
    [InlineData("RollbackDml")]
    [InlineData("RollbackCommit")]
    [InlineData("Vencido")]
    [InlineData("PasswordInvalida")]
    [InlineData("TokenClienteEnAdmin")]
    [InlineData("TokenAdminEnCliente")]
    [InlineData("TokenSustituido")]
    [InlineData("AdminExistenteActivo")]
    [InlineData("AdminExistenteSuspendido")]
    public async Task ActivacionAdmin_PersistenciaAislada(string caso)
    {
        var env = new ConcurrencyEnvironment();
        try
        {
            var f = await ArrangeAdmin(env);
            await env.VerifyAsync();
            if (caso == "Concurrente")
            {
                var (a, b) = await Race(env, 0, 0, "Personas",
                    async s => Outcome.From(await f.Service(s).ActivarAsync(f.Form())),
                    async s => Outcome.From(await f.Service(s).ActivarAsync(f.Form())));
                Assert.True(a.Success); Assert.False(b.Success);
                Assert.Equal(ActivacionAdminService.NoDisponible, b.Message);
                await VerifyActivated(env, f);
            }
            else if (caso.StartsWith("Rollback", StringComparison.Ordinal))
            {
                var before = await ConcurrencyFixtures.SnapshotAsync(env);
                var evidence = new WriteEvidence();
                var dml = new FailAfterWrite(evidence);
                var commit = new FailBeforeCommit(evidence);
                await using (var s = await env.OpenAsync(0, evidence,
                    caso == "RollbackDml" ? dml : (Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor)commit))
                    await Assert.ThrowsAsync<InjectedPersistenceFailure>(() => f.Service(s).ActivarAsync(f.Form()));
                Assert.True(evidence.Written);
                Assert.True(caso == "RollbackDml" ? dml.Fired : commit.Fired && evidence.IntegrityMaintained);
                Assert.True(before.SequenceEqual(await ConcurrencyFixtures.SnapshotAsync(env)), "Rollback changed persisted rows.");
                await using var read = await env.OpenAsync();
                Assert.True(await f.Service(read).ValidarAsync(f.Token));
            }
            else if (caso == "ActivarYRepetir")
            {
                await using (var s = await env.OpenAsync())
                    Assert.True((await f.Service(s).ActivarAsync(f.Form())).Exitoso);
                await VerifyActivated(env, f);
                var before = await ConcurrencyFixtures.SnapshotAsync(env);
                await using (var s = await env.OpenAsync())
                    Assert.False((await f.Service(s).ActivarAsync(f.Form())).Exitoso);
                Assert.True(before.SequenceEqual(await ConcurrencyFixtures.SnapshotAsync(env)), "Replay changed persisted rows.");
            }
            else
            {
                var token = f.Token;
                if (caso == "TokenClienteEnAdmin")
                {
                    // A real CLIENTE recovery token from the existing service and simulated transport.
                    var client = await ConcurrencyFixtures.CreateAsync(env);
                    await using var s = await env.OpenAsync();
                    var id = await s.Db.Usuarios.Where(u => u.PersonaId == client.Client).Select(u => u.Id).SingleAsync();
                    Assert.True(await s.Seguridad.SolicitarPasswordAsync(id));
                    token = env.Correo.UltimoToken;
                }
                if (caso == "TokenSustituido" || caso.StartsWith("AdminExistente", StringComparison.Ordinal))
                {
                    await using var setup = await env.OpenAsync();
                    if (caso == "TokenSustituido")
                    {
                        var e = await setup.Db.EnlacesPasswordCliente.SingleAsync(e => e.UsuarioId == f.Id);
                        e.TokenHash = (string)AdminHelper("HashToken", TokenInvitacion.Generar());
                    }
                    else
                    {
                        var u = await setup.Db.Usuarios.SingleAsync(u => u.Id == f.Id);
                        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Existing!9a");
                        u.Activo = caso == "AdminExistenteActivo";
                        u.PrimerLogin = false;
                    }
                    await setup.Db.SaveChangesAsync();
                }
                var before = await ConcurrencyFixtures.SnapshotAsync(env);
                await using (var s = await env.OpenAsync())
                {
                    if (caso == "TokenAdminEnCliente")
                    {
                        Assert.False(await s.Seguridad.ValidarEnlaceAsync(token));
                        Assert.False((await s.Seguridad.RestablecerAsync(new() { Token = token,
                            Password = f.Form().Password, ConfirmarPassword = f.Form().Password })).Exitoso);
                    }
                    else
                    {
                        var service = f.Service(s, caso == "Vencido" ? f.Created.AddMinutes(10) : null);
                        var vm = f.Form(token);
                        if (caso == "PasswordInvalida") vm.Password = vm.ConfirmarPassword = "Aa1!" + new string('x', 69);
                        Assert.False((await service.ActivarAsync(vm)).Exitoso);
                    }

                }
                Assert.True(before.SequenceEqual(await ConcurrencyFixtures.SnapshotAsync(env)), "Rejected operation changed persisted rows.");
            }
        }
        finally { await env.VerifyAsync(); }
    }
}
