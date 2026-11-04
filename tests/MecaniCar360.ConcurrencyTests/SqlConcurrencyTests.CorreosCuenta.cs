using MecaniCar360.Controllers;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Concurrency;

public sealed partial class SqlConcurrencyTests
{
    // Single theory, eight independent cases; no order, host, SMTP or main database dependency.
    [SqlConcurrencyTheory, Trait("Category", "SqlConcurrency"), Trait("Security", "SEC08")]
    [InlineData("PasswordSupresion")]
    [InlineData("PasswordConsumido")]
    [InlineData("PasswordConcurrente")]
    [InlineData("PasswordFallo")]
    [InlineData("InvitacionSupresion")]
    [InlineData("InvitacionConcurrente")]
    [InlineData("InvitacionFallo")]
    [InlineData("RespuestaNeutra")]
    public async Task CorreosCuenta_IntervaloPersistido(string caso)
    {
        var env = new ConcurrencyEnvironment();
        try
        {
            var f = await ConcurrencyFixtures.CreateAsync(env);
            int usuario;
            string email;
            await using (var read = await env.OpenAsync())
            {
                var u = await read.Db.Usuarios.AsNoTracking().SingleAsync(u => u.PersonaId == f.Client);
                usuario = u.Id; email = u.EmailLogin;
            }
            if (caso.StartsWith("Invitacion", StringComparison.Ordinal))
            {
                var persona = await PrepareInvitation(env, f);
                await InvitationCase(env, f, persona, caso);
            }
            else if (caso == "RespuestaNeutra")
            {
                await using (var s = await env.OpenAsync())
                {
                    var controller = new SeguridadClienteController(s.Seguridad, null!);
                    Assert.Equal("Solicitada", Assert.IsType<ViewResult>(await controller.Recuperar(new() { Email = email })).ViewName);
                }
                Assert.Equal(1, env.Correo.Intentos);
                await using (var setup = await env.OpenAsync())
                {
                    (await setup.Db.Usuarios.SingleAsync(u => u.Id == usuario)).Activo = false;
                    await setup.Db.SaveChangesAsync();
                }
                var before = await ConcurrencyFixtures.SnapshotAsync(env);
                foreach (var destino in new[] { email, f.Label + "absent@example.invalid" })
                {
                    await using var s = await env.OpenAsync();
                    var controller = new SeguridadClienteController(s.Seguridad, null!);
                    var view = Assert.IsType<ViewResult>(await controller.Recuperar(new() { Email = destino }));
                    Assert.Equal("Solicitada", view.ViewName);
                    Assert.Null(view.Model);
                }
                Assert.Equal(1, env.Correo.Intentos);
                Assert.True(before.SequenceEqual(await ConcurrencyFixtures.SnapshotAsync(env)), "Ineligible recovery changed persisted rows.");
            }
            else if (caso == "PasswordConcurrente")
            {
                var (a,b) = await Race(env, usuario, usuario, "Personas",
                    async s => new Outcome(await s.Seguridad.SolicitarPasswordAsync(usuario), "Authenticated issuance"),
                    async s => { await s.Seguridad.SolicitarRecuperacionAsync(email); return new Outcome(true, "Neutral public completion"); });
                Assert.True(a.Success); Assert.True(b.Success);
                Assert.Equal(1, env.Correo.Intentos);
                await VerifyPassword(env, usuario, false, false);
            }
            else
            {
                env.Correo.Fallar = caso == "PasswordFallo";
                await using (var s = await env.OpenAsync(usuario))
                    Assert.Equal(!env.Correo.Fallar, await s.Seguridad.SolicitarPasswordAsync(usuario));
                Assert.Equal(1, env.Correo.Intentos);
                var original = env.Correo.UltimoToken;
                await VerifyPassword(env, usuario, false, env.Correo.Fallar);
                if (caso == "PasswordConsumido")
                {
                    var password = "Fixture!9a" + Guid.NewGuid().ToString("N");
                    await using var s = await env.OpenAsync(usuario);
                    var r = await s.Seguridad.RestablecerAsync(new() { Token = original, Password = password, ConfirmarPassword = password });
                    Assert.True(r.Exitoso, "Fixture password consumption failed.");
                    await VerifyPassword(env, usuario, true, false);
                    Assert.Equal(1, await s.Db.Auditorias.CountAsync(a => a.Entidad == "Usuario" &&
                        a.EntidadId == usuario && a.UsuarioId == null && a.Accion == "PASSWORD_CLIENTE_RESTABLECIDA"));
                }
                var before = await ConcurrencyFixtures.SnapshotAsync(env);
                await using (var s = await env.OpenAsync(usuario))
                {
                    Assert.False(await s.Seguridad.SolicitarPasswordAsync(usuario));
                    await s.Seguridad.SolicitarRecuperacionAsync(email);
                    Assert.Equal(caso == "PasswordSupresion", await s.Seguridad.ValidarEnlaceAsync(original));
                }
                Assert.Equal(1, env.Correo.Intentos);
                Assert.True(before.SequenceEqual(await ConcurrencyFixtures.SnapshotAsync(env)), "Suppression changed persisted rows.");
                await AgePassword(env, usuario, f.Client, f.Label);
                env.Correo.Fallar = false;
                await using (var s = await env.OpenAsync(usuario))
                {
                    await s.Seguridad.SolicitarRecuperacionAsync(email);
                    Assert.Equal(2, env.Correo.Intentos);
                    Assert.True(original != env.Correo.UltimoToken, "Reissue did not replace token.");
                    Assert.False(await s.Seguridad.ValidarEnlaceAsync(original));
                }
                await VerifyPassword(env, usuario, false, false);
            }
            // Expected simulated failures are checked above. No SQL failure may hide behind a neutral response.
            Assert.Equal(caso is "PasswordFallo" or "InvitacionFallo" ? 1 : 0, env.Avisos.Cantidad);
        }
        finally { await env.VerifyAsync(); }
    }

    private static async Task VerifyPassword(ConcurrencyEnvironment env, int usuario, bool consumido, bool invalidado)
    {
        await using var read = await env.OpenAsync();
        var link = Assert.Single(await read.Db.EnlacesPasswordCliente.AsNoTracking().Where(e => e.UsuarioId == usuario).ToListAsync());
        Assert.Equal(consumido, link.FechaConsumida.HasValue);
        Assert.Equal(invalidado, link.FechaInvalidacion.HasValue);
        Assert.True(link.TokenHash == TokenInvitacion.Hash(env.Correo.UltimoToken), "Persisted token mismatch.");
        Assert.Equal(!consumido && !invalidado, await read.Seguridad.ValidarEnlaceAsync(env.Correo.UltimoToken));
        await env.VerifyAsync();
    }

    private static async Task AgePassword(ConcurrencyEnvironment env, int usuario, int persona, string label)
    {
        await using var s = await env.OpenAsync();
        Assert.True(await s.Db.Usuarios.AnyAsync(u => u.Id == usuario && u.PersonaId == persona && u.EmailLogin.StartsWith(label)));
        var link = await s.Db.EnlacesPasswordCliente.SingleAsync(e => e.UsuarioId == usuario);
        link.FechaCreacion = DateTime.UtcNow - EmisionCorreoCuenta.Intervalo - TimeSpan.FromSeconds(1);
        await s.Db.SaveChangesAsync();
    }

    private static async Task<int> PrepareInvitation(ConcurrencyEnvironment env, ConcurrencyFixtures f)
    {
        await using var s = await env.OpenAsync();
        var p = new Persona { Nombre = "Invitado", Apellido = f.Label, Activo = true,
            Email = f.Label + "invite@example.invalid", Telefono = "1123456789" };
        p.Roles.Add(new PersonaRol { Rol = await s.Db.Roles.SingleAsync(r => r.Nombre == "CLIENTE" && r.Activo) });
        s.Db.Personas.Add(p); await s.Db.SaveChangesAsync();
        return p.Id;
    }

    private static async Task InvitationCase(ConcurrencyEnvironment env, ConcurrencyFixtures f, int persona, string caso)
    {
        env.Correo.Fallar = caso == "InvitacionFallo";
        if (caso == "InvitacionConcurrente")
        {
            var (a,b) = await Race(env, f.Admin, f.Admin, "Personas",
                async s => Outcome.From(await s.Invitaciones.EmitirAsync(persona, f.Admin)),
                async s => Outcome.From(await s.Invitaciones.EmitirAsync(persona, f.Admin)));
            Assert.True(a.Success); Assert.True(b.Success);
        }
        else
        {
            await using var s = await env.OpenAsync(f.Admin);
            Assert.Equal(!env.Correo.Fallar, (await s.Invitaciones.EmitirAsync(persona, f.Admin)).Exitoso);
        }
        Assert.Equal(1, env.Correo.Intentos);
        var token = env.Correo.UltimoToken;
        await VerifyInvitation(env, persona, 1, env.Correo.Fallar, f.Admin);
        var before = await ConcurrencyFixtures.SnapshotAsync(env);
        await using (var s = await env.OpenAsync(f.Admin))
            Assert.True((await s.Invitaciones.EmitirAsync(persona, f.Admin)).Exitoso);
        Assert.Equal(1, env.Correo.Intentos);
        Assert.True(before.SequenceEqual(await ConcurrencyFixtures.SnapshotAsync(env)), "Suppressed invitation changed rows.");
        await using (var s = await env.OpenAsync())
        {
            Assert.True(await s.Db.Personas.AnyAsync(p => p.Id == persona && p.Email != null && p.Email.StartsWith(f.Label)));
            var invitation = await s.Db.InvitacionesCliente.SingleAsync(i => i.PersonaId == persona);
            invitation.FechaCreacion = DateTime.UtcNow - EmisionCorreoCuenta.Intervalo - TimeSpan.FromSeconds(1);
            invitation.FechaExpiracion = invitation.FechaCreacion.AddHours(48);
            await s.Db.SaveChangesAsync();
        }
        env.Correo.Fallar = false;
        await using (var s = await env.OpenAsync(f.Admin))
        {
            Assert.True((await s.Invitaciones.EmitirAsync(persona, f.Admin)).Exitoso);
            Assert.False(await s.Invitaciones.ValidarAsync(token));
        }
        Assert.Equal(2, env.Correo.Intentos);
        Assert.True(token != env.Correo.UltimoToken, "Invitation token was not replaced.");
        await VerifyInvitation(env, persona, 2, false, f.Admin);
    }

    private static async Task VerifyInvitation(ConcurrencyEnvironment env, int persona, int count, bool invalidada, int actor)
    {
        await using var s = await env.OpenAsync();
        var rows = await s.Db.InvitacionesCliente.AsNoTracking().Where(i => i.PersonaId == persona).OrderBy(i => i.Id).ToListAsync();
        Assert.Equal(count, rows.Count);
        Assert.Equal(invalidada, rows.Last().FechaInvalidacion.HasValue);
        Assert.All(rows, i => Assert.Null(i.FechaConsumida));
        Assert.All(rows.SkipLast(1), i => Assert.NotNull(i.FechaInvalidacion));
        Assert.True(rows.Last().TokenHash == TokenInvitacion.Hash(env.Correo.UltimoToken), "Invitation persistence mismatch.");
        Assert.Equal(!invalidada, await s.Invitaciones.ValidarAsync(env.Correo.UltimoToken));
        var ids = rows.Select(i => i.Id).ToArray();
        Assert.Equal(1, await s.Db.Auditorias.CountAsync(a => a.UsuarioId == actor && a.Entidad == "InvitacionCliente" && ids.Contains(a.EntidadId ?? 0) && a.Accion == "INVITACION_EMITIDA"));
        Assert.Equal(count - 1, await s.Db.Auditorias.CountAsync(a => a.UsuarioId == actor && a.Entidad == "InvitacionCliente" && ids.Contains(a.EntidadId ?? 0) && a.Accion == "INVITACION_REEMITIDA"));
        if (invalidada)
            Assert.Equal(1, await s.Db.Auditorias.CountAsync(a => a.UsuarioId == actor && a.Entidad == "InvitacionCliente" && ids.Contains(a.EntidadId ?? 0) && a.Accion == "INVITACION_ENVIO_FALLIDO"));
        await env.VerifyAsync();
    }
}
