using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Xunit.Abstractions;

namespace MecaniCar360.Tests;

// Opt-in: never migrates, recreates, repairs integrity or sends external email.
// Run with MC360_INTEGRATION_12C1=1 and MC360_TEST_CONNECTION pointing to the
// explicitly authorized local development database. All fixtures are removed via EF.
public sealed class Sql12C1FactAttribute : FactAttribute
{
    public Sql12C1FactAttribute()
    {
        if (Environment.GetEnvironmentVariable("MC360_INTEGRATION_12C1") != "1")
            Skip = "Requires explicit local SQL integration opt-in.";
    }
}

public sealed class OnboardingSqlIntegrationTests(ITestOutputHelper output)
{
    [Sql12C1Fact]
    public async Task RegistroCompleto_IdentidadInvitacionesDniContactoYRollback_SQLServer()
    {
        var cs = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MC360_TEST_CONNECTION"));
        Assert.True(cs.DataSource == @"BOOK-JKTL5UI49E\CABRALPEDRO" && cs.InitialCatalog == "MecaniCar360" && cs.IntegratedSecurity,
            "Only the explicitly authorized local development database is allowed.");
        var prefix = "TEST12C1_" + Guid.NewGuid().ToString("N")[..12];
        var password = "Fixture!9a" + Guid.NewGuid().ToString("N");
        Func<string, string> Email = suffix => prefix + suffix + "@example.invalid";
        string Dni(int n) => "9" + DateTime.UtcNow.ToString("yyMMddHHmmss") + n;
        var options = new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(cs.ConnectionString).Options;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Invitaciones:UrlBase"] = "http://localhost:5190", ["Smtp:Host"] = "127.0.0.1", ["Smtp:Port"] = "1",
            ["Smtp:User"] = "fixture@example.invalid", ["Smtp:Password"] = "fixture", ["Smtp:EnableSsl"] = "false"
        }).Build();
        var mail = new CapturedMail(config);
        using var services = new ServiceCollection().AddLogging().AddHttpContextAccessor()
            .AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider())
            .AddDbContext<MecaniCarContext>(b => b.UseSqlServer(cs.ConnectionString))
            .AddSingleton<IConfiguration>(config).AddSingleton<IWebHostEnvironment>(new TestEnvironment())
            .AddSingleton<EmailService>(mail).AddScoped<PermisoService>().AddScoped<AuditoriaService>()
            .AddScoped<IdentidadClienteService>().AddScoped<ClienteHabilitadoService>().AddScoped<AccountService>()
            .AddScoped<GoogleClienteService>().AddScoped<RegistroCompletoClienteService>()
            .AddScoped<RegistroClienteService>().AddScoped<InvitacionClienteService>().AddScoped<AltaClienteService>()
            .AddScoped<PresentacionCuentaService>().AddScoped<SeguridadClienteService>().BuildServiceProvider();
        async Task<T> Use<S, T>(Func<S, Task<T>> action, int actor = 0) where S : notnull
        {
            using var scope = services.CreateScope();
            var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = new DefaultHttpContext { User = actor == 0 ? new() : new(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, actor.ToString()) }, "fixture")) };
            try { return await action(scope.ServiceProvider.GetRequiredService<S>()); }
            finally { accessor.HttpContext = null; }
        }
        void Check(bool value, string label) { Assert.True(value, label); output.WriteLine("PASS " + label); }
        async Task Integrity(string label)
        {
            await using var db = new MecaniCarContext(options);
            await db.Database.OpenConnectionAsync();
            Check((await new IntegridadService(db).VerificarAsync()).EsValida, "DVH/DVV " + label);
        }
        async Task<ServiceResult<ClaimsIdentity>> Google(string sub, string email, int? link = null, bool verified = true)
        {
            string? stamp = null;
            if (link.HasValue) { await using var db = new MecaniCarContext(options); stamp = await db.Usuarios.Where(u => u.Id == link).Select(u => u.SecurityStamp).SingleAsync(); }
            return await Use<GoogleClienteService, ServiceResult<ClaimsIdentity>>(s =>
                (Task<ServiceResult<ClaimsIdentity>>)typeof(GoogleClienteService).GetMethod("ResolverAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(s, new object?[] { new ClaimsPrincipal(new ClaimsIdentity(new[] {
                        new Claim(ClaimTypes.NameIdentifier, sub), new Claim(ClaimTypes.Email, email),
                        new Claim(GoogleClienteConfiguracion.EmailVerificado, verified.ToString()),
                        new Claim(ClaimTypes.GivenName, "Fixture"), new Claim(ClaimTypes.Surname, "Google") }, "Google")), link, stamp })!, link ?? 0);
        }
        Task<ServiceResult> Register(string email) => Use<RegistroClienteService, ServiceResult>(s =>
            s.SolicitarAsync(new() { Nombre = "Fixture", Apellido = "Correo", Email = email }));
        RegistroCompletoClienteViewModel Complete(string dni) => new() { Nombre = "Fixture", Apellido = "Completo", Dni = dni,
            Telefono = "1123456789", Email = "tampered@example.invalid" };
        ActivarClienteViewModel Activation(string email, string dni, string suffix) => new() { Token = mail.Tokens[email],
            Nombre = "Fixture", Apellido = "Activado", Dni = dni, Telefono = "1123456789", Username = prefix + suffix,
            Password = password, ConfirmarPassword = password, Email = "tampered@example.invalid" };

        await Integrity("before");
        await using (var db = new MecaniCarContext(options)) {
            Check(!db.Database.HasPendingModelChanges(), "model matches snapshot");
            Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "all migrations applied");
        }
        output.WriteLine("FIXTURE " + prefix);
        try
        {
            config["Invitaciones:UrlBase"] = null;
            try {
                var missingUrl = await Register(Email("missingUrl"));
                Check(missingUrl.Exitoso && missingUrl.Mensaje == RegistroClienteService.Respuesta,
                    "missing URL keeps neutral public response");
                await using var db = new MecaniCarContext(options);
                var p = await db.Personas.SingleAsync(p => p.Email == Email("missingUrl"));
                Check(!await db.InvitacionesCliente.AnyAsync(i => i.PersonaId == p.Id) && !mail.Tokens.ContainsKey(Email("missingUrl")),
                    "missing URL creates no invitation and makes no email attempt");
            }
            finally { config["Invitaciones:UrlBase"] = "http://localhost:5190"; }
            var g = await Google(prefix + "subject", Email("g"));
            Check(g.Exitoso, "Google verified identity creates pending CLIENTE (simulated provider)");
            var googleId = int.Parse(g.Data!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            int googlePersona;
            await using (var db = new MecaniCarContext(options)) {
                var u = await db.Usuarios.SingleAsync(u => u.Id == googleId); googlePersona = u.PersonaId;
                Check(u.PrimerLogin && u.PasswordHash == null, "pending Google account has no local password");
                Check(!await new ClienteHabilitadoService(db).EstaHabilitadoAsync(u.PersonaId), "pending client blocked");
                Check(!await new PermisoService(db).TienePermisoAsync(u.Id, "CLIENTE_TURNO_CREAR"), "pending client has no business permission");
                var http = new DefaultHttpContext { User = new(g.Data!) }; http.Request.Method = "GET";
                http.Request.Path = "/PortalCliente/Index"; var continued = false;
                await new MecaniCar360.Middleware.PrimerLoginMiddleware(_ => { continued = true; return Task.CompletedTask; }).InvokeAsync(http, db);
                Check(!continued && http.Response.Headers.Location.ToString() == "/RegistroCompletoCliente/Index", "pending Google middleware blocks direct portal route");
            }
            var resumed = await Google(prefix + "subject", Email("g"));
            Check(resumed.Exitoso && resumed.Data!.FindFirst(ClaimTypes.NameIdentifier)!.Value == googleId.ToString(), "resume same Google Usuario");
            foreach (var field in new[] { "Nombre", "Apellido", "Dni", "Telefono" }) {
                var invalid = Complete(Dni(1)); typeof(RegistroCompletoClienteViewModel).GetProperty(field)!.SetValue(invalid, "");
                Check(!(await Use<RegistroCompletoClienteService, ServiceResult>(s => s.CompletarAsync(googleId, invalid), googleId)).Exitoso,
                    "required " + field);
            }
            var firstDni = Dni(1);
            Check((await Use<RegistroCompletoClienteService, ServiceResult>(s => s.CompletarAsync(googleId, Complete("0" + firstDni)), googleId)).Exitoso,
                "Google completion does not require local credential");
            await using (var db = new MecaniCarContext(options)) {
                var u = await db.Usuarios.Include(u => u.Persona).SingleAsync(u => u.Id == googleId);
                Check(u.PersonaId == googlePersona && string.Equals(u.EmailLogin, Email("g"), StringComparison.OrdinalIgnoreCase) && u.Persona.Email == u.EmailLogin,
                    "verified email immutable, same Persona");
                Check(u.Persona.Dni == firstDni && !u.PrimerLogin && u.PasswordHash == null, "normalized DNI and Google-only state persisted");
                Check(await new ClienteHabilitadoService(db).EstaHabilitadoAsync(u.PersonaId), "completed client enabled");
            }
            var googleOnly = await Use<PresentacionCuentaService, PresentacionCuenta>(s => s.ObtenerAsync(googleId));
            Check(!googleOnly.PasswordLocal && googleOnly.PuedeAgregarPassword && !googleOnly.PuedeVincularGoogle,
                "Google-only account offers add password, not link Google");
            Check((await Google(prefix + "subject", Email("g"))).Exitoso, "Google-only account can sign in again");
            await Use<SeguridadClienteService, bool>(async s => { await s.SolicitarRecuperacionAsync(" " + Email("g").ToUpperInvariant() + " "); return true; });
            var googleRecovery = mail.PasswordTokens[Email("g")];
            Check(await Use<SeguridadClienteService, bool>(s => s.ValidarEnlaceAsync(googleRecovery)),
                "Google-only verified account can request recovery with normalized email");
            await using (var db = new MecaniCarContext(options)) {
                var u = await db.Usuarios.SingleAsync(u => u.Id == googleId);
                u.SecurityStamp = Guid.NewGuid().ToString("N"); await db.SaveChangesAsync();
            }
            Check(!await Use<SeguridadClienteService, bool>(s => s.ValidarEnlaceAsync(googleRecovery)),
                "security stamp change invalidates pending password link");
            Check(await Use<SeguridadClienteService, bool>(s => s.SolicitarPasswordAsync(googleId), googleId), "send recent verification for add password");
            var proof = mail.PasswordTokens[Email("g")];
            await using (var db = new MecaniCarContext(options))
            {
                var e = await db.EnlacesPasswordCliente.SingleAsync(e => e.UsuarioId == googleId);
                Check(e.TokenHash == TokenInvitacion.Hash(proof) && e.TokenHash != proof &&
                    e.FechaExpiracion - e.FechaCreacion == TimeSpan.FromMinutes(10), "only token hash stored, lifetime ten minutes");
                e.FechaCreacion = DateTime.UtcNow.AddMinutes(-11); e.FechaExpiracion = DateTime.UtcNow.AddMinutes(-1);
                await db.SaveChangesAsync();
                Check(!(await Use<SeguridadClienteService, ServiceResult>(s => s.RestablecerAsync(
                    new() { Token = proof, Password = password, ConfirmarPassword = password }))).Exitoso,
                    "expired email proof cannot establish credentials");
            }
            Check(await Use<SeguridadClienteService, bool>(s => s.SolicitarPasswordAsync(googleId), googleId), "reissue password link");
            Check(!await Use<SeguridadClienteService, bool>(s => s.ValidarEnlaceAsync(proof)), "new link invalidates previous link");
            proof = mail.PasswordTokens[Email("g")];
            await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Use<SeguridadClienteService, bool>(s => s.SolicitarPasswordAsync(googleId))));
            await using (var db = new MecaniCarContext(options)) {
                var current = await db.EnlacesPasswordCliente.SingleAsync(e => e.UsuarioId == googleId);
                proof = mail.PasswordHistory.Single(t => TokenInvitacion.Hash(t) == current.TokenHash);
                Check(await db.EnlacesPasswordCliente.CountAsync(e => e.UsuarioId == googleId) == 1,
                    "concurrent issuance keeps only one current password link");
            }
            Check(await Use<SeguridadClienteService, string?>(s => s.OrientarAsync(proof)) == null, "proof purpose cannot be substituted");
            var addPassword = new RestablecerPasswordClienteViewModel { Token = proof, Password = password, ConfirmarPassword = password };
            Check(!await Use<SeguridadClienteService, bool>(s => s.ValidarEnlaceAsync(new string('A', 43))), "altered password link rejected");
            var credentialRace = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
                Use<SeguridadClienteService, ServiceResult>(s => s.RestablecerAsync(addPassword))));
            Check(credentialRace.Count(r => r.Exitoso) == 1,
                "concurrent email proof establishes password exactly once on same account");
            Check(!(await Use<SeguridadClienteService, ServiceResult>(s => s.RestablecerAsync(addPassword))).Exitoso,
                "proof consumed by security stamp rotation");
            await Use<SeguridadClienteService, bool>(async s => { await s.SolicitarRecuperacionAsync(Email("g")); return true; });
            var disabledToken = mail.PasswordTokens[Email("g")];
            await using (var db = new MecaniCarContext(options)) {
                var u = await db.Usuarios.SingleAsync(u => u.Id == googleId); u.Activo = false; await db.SaveChangesAsync();
            }
            Check(!await Use<SeguridadClienteService, bool>(s => s.ValidarEnlaceAsync(disabledToken)) &&
                !await Use<SeguridadClienteService, bool>(s => s.SolicitarPasswordAsync(googleId)), "disabled account cannot issue or use password link");
            await using (var db = new MecaniCarContext(options)) {
                var u = await db.Usuarios.SingleAsync(u => u.Id == googleId); u.Activo = true; await db.SaveChangesAsync();
            }
            var local = await Use<AccountService, LoginResult>(s => s.LoginAsync(Email("g"), password));
            var returned = await Google(prefix + "subject", Email("g"));
            Check(local.Exitoso && local.Usuario!.Id == googleId && local.Usuario.PersonaId == googlePersona &&
                returned.Data!.FindFirst(ClaimTypes.NameIdentifier)!.Value == googleId.ToString(), "both methods recover same Usuario and Persona");
            Check(!(await Google(prefix + "other", Email("g"))).Exitoso, "no automatic link by email");
            Check(!(await Google(prefix + "unverified", Email("bad"), verified: false)).Exitoso, "unverified Google email rejected");
            var presentation = await Use<PresentacionCuentaService, PresentacionCuenta>(s => s.ObtenerAsync(googleId));
            Check(presentation.Nombre == "Fixture Completo" && presentation.PasswordLocal && !presentation.PuedeVincularGoogle,
                "presentation reflects completed Google plus local account");
            await Integrity("after Google completion");

            var repeated = await Task.WhenAll(new[] { Email("g"), "  " + Email("g").ToUpperInvariant() + "  " }
                .Select(Register));
            Check(repeated.All(r => r.Exitoso && r.Mensaje == RegistroClienteService.Respuesta),
                "concurrent existing Google email requests keep neutral response");
            async Task NoDuplicateGoogle()
            {
                await using var db = new MecaniCarContext(options);
                var email = Email("g").ToUpperInvariant();
                Check(await db.Usuarios.CountAsync(u => u.EmailLogin.Trim().ToUpper() == email) == 1 &&
                    await db.Personas.CountAsync(p => p.Email != null && p.Email.Trim().ToUpper() == email) == 1 &&
                    !await db.InvitacionesCliente.AnyAsync(i => i.EmailDestino.ToUpper() == email) &&
                    !mail.Tokens.ContainsKey(Email("g")),
                    "existing Google email creates no Persona, Usuario or invitation token");
            }
            await NoDuplicateGoogle();
            Check((await Use<SeguridadClienteService, string?>(s => s.OrientarAsync(mail.Codes[Email("g")])))?
                .Contains("Google") == true, "verified email receives account-specific guidance");
            Check(await Use<SeguridadClienteService, string?>(s => s.OrientarAsync("altered")) == null,
                "unverified request receives no account-specific guidance");

            Check((await Register(Email("local"))).Exitoso, "public email registration");
            var activation = Activation(Email("local"), Dni(2), "local");
            Check(await Use<InvitacionClienteService, bool>(s => s.ValidarAsync(activation.Token)), "valid token accepted without consumption");
            await using (var db = new MecaniCarContext(options)) {
                var i = await db.InvitacionesCliente.SingleAsync(i => i.EmailDestino == Email("local").ToLowerInvariant());
                Check(i.FechaConsumida == null && i.EmitidaPorUsuarioId == null && !await db.Usuarios.AnyAsync(u => u.PersonaId == i.PersonaId),
                    "public pending invitation has no Usuario and no consumption");
            }
            var altered = Activation(Email("local"), Dni(2), "local"); altered.Token = TokenInvitacion.Generar();
            Check(!(await Use<InvitacionClienteService, ServiceResult>(s => s.ActivarAsync(altered))).Exitoso, "altered token rejected");
            Check((await Use<InvitacionClienteService, ServiceResult>(s => s.ActivarAsync(activation))).Exitoso, "email confirmation and activation");
            Check(!(await Use<InvitacionClienteService, ServiceResult>(s => s.ActivarAsync(activation))).Exitoso, "used token rejected");
            int localId;
            await using (var db = new MecaniCarContext(options)) {
                var u = await db.Usuarios.SingleAsync(u => u.EmailLogin == Email("local").ToLowerInvariant()); localId = u.Id;
                var p = await db.Personas.SingleAsync(p => p.Id == u.PersonaId);
                Check(p.Nombre == activation.Nombre && p.Apellido == activation.Apellido && p.Dni == activation.Dni && p.Telefono == activation.Telefono &&
                    await new ClienteHabilitadoService(db).EstaHabilitadoAsync(p.Id), "activation persists complete Persona and enables client");
                Check(await db.Personas.CountAsync(p => p.Email == u.EmailLogin) == 1 && await db.Usuarios.CountAsync(x => x.PersonaId == u.PersonaId) == 1,
                    "one Persona and one Usuario after activation");
                Check(await db.Auditorias.AnyAsync(a => a.Entidad == "Usuario" && a.EntidadId == u.Id && a.Accion == "CLIENTE_ACTIVADO"), "activation audit");
            }
            var localPresentation = await Use<PresentacionCuentaService, PresentacionCuenta>(s => s.ObtenerAsync(localId));
            Check(localPresentation.PasswordLocal && localPresentation.PuedeVincularGoogle, "local-only authentication options");
            Check(!(await Google(prefix + "subject", Email("local"), localId)).Exitoso,
                "already linked subject cannot move to another account");
            Check((await Google(prefix + "localSubject", Email("local"), localId)).Exitoso,
                "local account links free verified Google identity");
            Check(!(await Use<PresentacionCuentaService, PresentacionCuenta>(s => s.ObtenerAsync(localId))).PuedeVincularGoogle,
                "link option disappears after successful linking");
            var change = new CambiarContraseñaViewModel { ContraseñaActual = "incorrecta", NuevaContraseña = password + "2", ConfirmarNuevaContraseña = password + "2" };
            Check(!(await Use<AccountService, ServiceResult<ClaimsIdentity>>(s => s.CambiarContraseñaAsync(change, localId), localId)).Exitoso,
                "password change rejects missing recent proof of current password");
            change.ContraseñaActual = password;
            Check((await Use<AccountService, ServiceResult<ClaimsIdentity>>(s => s.CambiarContraseñaAsync(change, localId), localId)).Exitoso,
                "existing password change verifies current credential and succeeds");
            Check((await Use<AccountService, LoginResult>(s => s.LoginAsync(Email("local"), password + "2"))).Usuario?.Id == localId,
                "password change preserves account identity");
            Check((await Use<RegistroCompletoClienteService, ServiceResult>(s => s.ActualizarContactoAsync(localId,
                new() { Telefono = "1198765432", Nombre = "Injected", Email = Email("g"), Dni = firstDni }), localId)).Exitoso, "own contact update");
            await using (var db = new MecaniCarContext(options)) {
                var u = await db.Usuarios.Include(u => u.Persona).SingleAsync(u => u.Id == localId);
                Check(u.Persona.Telefono == "1198765432" && u.Persona.Nombre == "Fixture" && u.Persona.Dni == activation.Dni,
                    "contact contract ignores identity fields");
                Check((await db.Personas.FindAsync(googlePersona))!.Telefono == "1123456789", "other client's contact unchanged");
            }
            await Register(Email("expired"));
            await using (var db = new MecaniCarContext(options)) {
                var i = await db.InvitacionesCliente.SingleAsync(i => i.EmailDestino == Email("expired").ToLowerInvariant());
                i.FechaCreacion = DateTime.UtcNow.AddHours(-49);
                i.FechaExpiracion = i.FechaCreacion.AddHours(48); await db.SaveChangesAsync();
            }
            Check(!(await Use<InvitacionClienteService, ServiceResult>(s => s.ActivarAsync(Activation(Email("expired"), Dni(3), "expired")))).Exitoso,
                "expired token rejected");

            await Register(Email("dup"));
            var duplicate = Activation(Email("dup"), "0" + firstDni, "dup");
            var rejected = await Use<InvitacionClienteService, ServiceResult>(s => s.ActivarAsync(duplicate));
            Check(!rejected.Exitoso && rejected.Mensaje == DniPersona.Error, "duplicate normalized DNI rejected without identity disclosure");
            await using (var db = new MecaniCarContext(options)) {
                var p = await db.Personas.SingleAsync(p => p.Email == Email("dup").ToLowerInvariant());
                Check(p.Dni == null && !await db.Usuarios.AnyAsync(u => u.PersonaId == p.Id) &&
                    await db.InvitacionesCliente.AnyAsync(i => i.PersonaId == p.Id && i.FechaConsumida == null), "duplicate leaves pending invitation, no partial activation");
                p.Dni = firstDni;
                var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
                Check(error.InnerException is SqlException { Number: 2601 or 2627 }, "SQL UNIQUE independently rejects duplicate DNI");
            }
            await Integrity("after SQL rejection");
            await Register(Email("race1")); await Register(Email("race2"));
            var sharedDni = Dni(4);
            var racers = await Task.WhenAll(
                Use<InvitacionClienteService, ServiceResult>(s => s.ActivarAsync(Activation(Email("race1"), sharedDni, "race1"))),
                Use<InvitacionClienteService, ServiceResult>(s => s.ActivarAsync(Activation(Email("race2"), "0" + sharedDni, "race2"))));
            Check(racers.Count(r => r.Exitoso) == 1, "concurrent activation same normalized DNI: exactly one winner");
            await using (var db = new MecaniCarContext(options)) {
                Check(await db.Personas.CountAsync(p => p.Dni == sharedDni) == 1 &&
                    await db.Usuarios.CountAsync(u => u.EmailLogin == Email("race1").ToLowerInvariant() || u.EmailLogin == Email("race2").ToLowerInvariant()) == 1,
                    "concurrency no duplicate Usuario or partial second activation");
            }
            await Integrity("after concurrency");

            var googleRace1 = await Google(prefix + "gr1", Email("gr1"));
            var googleRace2 = await Google(prefix + "gr2", Email("gr2"));
            Check(googleRace1.Exitoso && googleRace2.Exitoso, "two pending Google fixtures for completion race");
            var gr1 = int.Parse(googleRace1.Data!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var gr2 = int.Parse(googleRace2.Data!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            Check(!(await Use<RegistroCompletoClienteService, ServiceResult>(s => s.CompletarAsync(gr1, Complete(firstDni)), gr1)).Exitoso,
                "Google completion cannot take another Persona by DNI");
            var completionDni = Dni(7);
            var completions = await Task.WhenAll(
                Use<RegistroCompletoClienteService, ServiceResult>(s => s.CompletarAsync(gr1, Complete(completionDni)), gr1),
                Use<RegistroCompletoClienteService, ServiceResult>(s => s.CompletarAsync(gr2, Complete("0" + completionDni)), gr2));
            Check(completions.Count(r => r.Exitoso) == 1, "concurrent Google completion same DNI: one winner");
            await using (var db = new MecaniCarContext(options)) {
                var accounts = await db.Usuarios.Include(u => u.Persona).Where(u => u.Id == gr1 || u.Id == gr2).ToListAsync();
                var loser = accounts.Single(u => u.PrimerLogin);
                Check(loser.PasswordHash == null && loser.Persona.Dni == null &&
                    !await db.Auditorias.AnyAsync(a => a.Entidad == "Usuario" && a.EntidadId == loser.Id && a.Accion == "REGISTRO_CLIENTE_COMPLETADO"),
                    "losing Google completion leaves no password, DNI or success audit");
            }
            await Integrity("after Google completion race");

            int caja;
            await using (var db = new MecaniCarContext(options)) {
                var u = new Usuario { Username = prefix + "caja", EmailLogin = Email("caja").ToLowerInvariant(), PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    PrimerLogin = false, Persona = new Persona { Nombre = "Fixture", Apellido = "Caja", Email = Email("caja").ToLowerInvariant() } };
                u.Persona.Roles.Add(new PersonaRol { RolId = await db.Roles.Where(r => r.Nombre == "CAJA" && r.Activo).Select(r => r.Id).SingleAsync() });
                db.Usuarios.Add(u); await db.SaveChangesAsync(); caja = u.Id;
            }
            Check((await Use<AccountService, LoginResult>(s => s.LoginAsync(Email("caja"), password))).Exitoso, "CAJA credentials unchanged");
            Check(!(await Google(prefix + "internal", Email("caja"))).Exitoso && !(await Google(prefix + "link", Email("caja"), caja)).Exitoso,
                "Google cannot create or link an internal identity");
            Check(!await Use<SeguridadClienteService, bool>(s => s.PuedeGestionarAsync(caja)), "internal account excluded from client credential setup");
            var created = await Use<AltaClienteService, ServiceResult<ClienteOperativoViewModel>>(s => s.CrearAsync(new() {
                Nombre = "Fixture", Apellido = "Presencial", Dni = Dni(5), Email = Email("cajaClient"), Telefono = "1123456789" }, caja), caja);
            Check(created.Exitoso, "CAJA retains in-person client permission");
            var presencial = created.Data!.Id;
            var issued = await Use<InvitacionClienteService, ServiceResult>(s => s.EmitirAsync(presencial, caja), caja);
            Check(issued.Exitoso, "CAJA issues invitation through existing permissions");
            Check(!(await Use<InvitacionClienteService, ServiceResult>(s => s.EmitirAsync(presencial, localId), localId)).Exitoso, "CLIENTE cannot issue administrative invitations");
            int cajaPersona;
            await using (var db = new MecaniCarContext(options)) cajaPersona = await db.Usuarios.Where(u => u.Id == caja).Select(u => u.PersonaId).SingleAsync();
            Check(!(await Use<InvitacionClienteService, ServiceResult>(s => s.EmitirAsync(cajaPersona, caja), caja)).Exitoso, "internal Persona cannot be invited as CLIENTE");
            Check((await Use<InvitacionClienteService, ServiceResult>(s => s.ActivarAsync(Activation(Email("cajaClient"), Dni(5), "presencial")))).Exitoso, "CAJA invitation activation");
            await using (var db = new MecaniCarContext(options)) Check(await db.Usuarios.AnyAsync(u => u.PersonaId == presencial) &&
                await db.Personas.CountAsync(p => p.Email == Email("cajaClient")) == 1, "in-person activation reuses exact Persona");
            await HttpScenarios();
            await Integrity("after all successful and rejected writes");

            async Task HttpScenarios()
            {
                var repo = new DirectoryInfo(AppContext.BaseDirectory);
                while (repo != null && !File.Exists(Path.Combine(repo.FullName, "MecaniCar360.csproj"))) repo = repo.Parent;
                Assert.NotNull(repo);
                var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
                var dll = Path.Combine(repo!.FullName, "bin", configuration, "net8.0", "MecaniCar360.dll");
                Assert.True(File.Exists(dll), "Build the application in the same configuration before HTTP integration.");
                var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
                var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
                var url = new Uri($"http://127.0.0.1:{port}");
                using var smtp = new LocalMail();
                var start = new ProcessStartInfo("dotnet") { WorkingDirectory = repo.FullName, UseShellExecute = false,
                    CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                start.ArgumentList.Add(dll); start.ArgumentList.Add("--urls"); start.ArgumentList.Add(url.ToString());
                start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
                start.Environment["ConnectionStrings__DefaultConnection"] = cs.ConnectionString;
                start.Environment["Authentication__Google__ClientId"] = "";
                start.Environment["Authentication__Google__ClientSecret"] = "";
                start.Environment["Logging__LogLevel__Default"] = "Error";
                foreach (var item in config.AsEnumerable()) if (item.Value != null) start.Environment[item.Key.Replace(":", "__")] = item.Value;
                start.Environment["Smtp__Port"] = smtp.Port.ToString();
                using var app = new Process { StartInfo = start };
                // Never print request bodies, cookies, credentials, tokens or application logs.
                app.OutputDataReceived += (_, _) => { }; app.ErrorDataReceived += (_, _) => { };
                app.Start(); app.BeginOutputReadLine(); app.BeginErrorReadLine();
                HttpClient Client() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) {
                    BaseAddress = url, Timeout = TimeSpan.FromSeconds(30) };
                string Anti(string html) => WebUtility.HtmlDecode(Regex.Match(html,
                    "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
                async Task<string> Page(HttpClient client, string path)
                {
                    var result = await client.GetAsync(path);
                    for (var retry = 0; result.StatusCode == HttpStatusCode.TooManyRequests && retry < 4; retry++) {
                        result.Dispose(); await Task.Delay(3500); result = await client.GetAsync(path);
                    }
                    Check(result.StatusCode == HttpStatusCode.OK, "HTTP GET " + path + " status=" + (int)result.StatusCode);
                    return await result.Content.ReadAsStringAsync();
                }
                async Task<HttpResponseMessage> Post(HttpClient client, string path, string anti, Dictionary<string, string> values)
                {
                    if (anti.Length > 0) values["__RequestVerificationToken"] = anti;
                    var result = await client.PostAsync(path, new FormUrlEncodedContent(values));
                    // Respect the real rate limit; retry only 429 (action was not executed).
                    for (var retry = 0; result.StatusCode == HttpStatusCode.TooManyRequests && retry < 4; retry++) {
                        result.Dispose(); await Task.Delay(3500); result = await client.PostAsync(path, new FormUrlEncodedContent(values));
                    }
                    return result;
                }
                try
                {
                    using var client = Client();
                    var ready = false;
                    for (var n = 0; n < 60 && !app.HasExited; n++) {
                        try { ready = (await client.GetAsync("/Account/Login")).StatusCode == HttpStatusCode.OK; } catch (HttpRequestException) { }
                        if (ready) break; await Task.Delay(500);
                    }
                    Check(ready, "application startup with valid integrity");
                    var registration = await Page(client, "/RegistroCliente/Index");
                    var duplicate = await Post(client, "/RegistroCliente/Index", Anti(registration), new() {
                        ["Nombre"] = "Fixture", ["Apellido"] = "Duplicado", ["Email"] = "  " + Email("g").ToUpperInvariant() + "  " });
                    Check(duplicate.StatusCode == HttpStatusCode.OK &&
                        WebUtility.HtmlDecode(await duplicate.Content.ReadAsStringAsync()).Contains(RegistroClienteService.Respuesta),
                        "HTTP existing Google email returns neutral public response");
                    await NoDuplicateGoogle();
                    var orientation = await Page(client, "/RegistroCliente/Orientacion");
                    Check((await Post(client, "/RegistroCliente/Orientacion", "", new() { ["Codigo"] = smtp.Code })).StatusCode == HttpStatusCode.BadRequest,
                        "guidance requires antiforgery");
                    var guide = await Post(client, "/RegistroCliente/Orientacion", Anti(orientation), new() { ["Codigo"] = smtp.Code });
                    Check(WebUtility.HtmlDecode(await guide.Content.ReadAsStringAsync()).Contains("Inicie sesión mediante Google"),
                        "HTTP guidance disclosed only after email proof");
                    var anonymous = await client.GetAsync("/PortalCliente/MisDatos");
                    Check(anonymous.StatusCode == HttpStatusCode.Redirect && anonymous.Headers.Location!.ToString().Contains("Login"), "anonymous portal rejected");
                    await Register(Email("http"));
                    var data = Activation(Email("http"), Dni(6), "http");
                    var request = new HttpRequestMessage(HttpMethod.Get, "/ActivacionCliente/Index");
                    request.Headers.Add("X-Invitacion", data.Token);
                    var validation = await client.SendAsync(request);
                    var form = await validation.Content.ReadAsStringAsync();
                    Check(validation.StatusCode == HttpStatusCode.OK && form.Contains("ConfirmarPassword"), "activation HTTP header validation renders complete form");
                    var fields = new Dictionary<string, string> { ["Token"] = data.Token, ["Nombre"] = data.Nombre,
                        ["Apellido"] = data.Apellido, ["Dni"] = data.Dni, ["Telefono"] = data.Telefono, ["Email"] = "tampered@example.invalid",
                        ["Username"] = data.Username, ["Password"] = password, ["ConfirmarPassword"] = password };
                    Check((await Post(client, "/ActivacionCliente/Index", "", new(fields))).StatusCode == HttpStatusCode.BadRequest,
                        "activation without antiforgery rejected");
                    Check(await Use<InvitacionClienteService, bool>(s => s.ValidarAsync(data.Token)), "rejected HTTP POST leaves invitation usable");
                    var activated = await Post(client, "/ActivacionCliente/Index", Anti(form), fields);
                    Check(activated.StatusCode == HttpStatusCode.Redirect && activated.Headers.Location!.ToString().Contains("Completada"), "activation HTTP POST succeeds");
                    var loginForm = await Page(client, "/Account/Login");
                    var logged = await Post(client, "/Account/Login", Anti(loginForm), new() { ["username"] = Email("http"), ["password"] = password });
                    Check(logged.StatusCode == HttpStatusCode.Redirect && logged.Headers.Location!.ToString().StartsWith("/PortalCliente"), "real cookie login reaches portal");
                    var portal = await Page(client, "/PortalCliente/Index");
                    Check(portal.Contains("Fixture") && portal.Contains("Activado"), "portal displays name and surname");
                    var contact = await Page(client, "/PortalCliente/MisDatos");
                    Check(!Regex.IsMatch(contact, "<li[^>]+style=", RegexOptions.IgnoreCase), "empty validation summary does not emit CSP-blocked inline style");
                    Check((await Post(client, "/PortalCliente/MisDatos", "", new() { ["Telefono"] = "1166666666" })).StatusCode == HttpStatusCode.BadRequest,
                        "contact without antiforgery rejected");
                    var changed = await Post(client, "/PortalCliente/MisDatos", Anti(contact), new() {
                        ["Telefono"] = "1177777777", ["PersonaId"] = googlePersona.ToString(), ["UsuarioId"] = googleId.ToString(),
                        ["Email"] = Email("g"), ["Dni"] = firstDni, ["Nombre"] = "Injected" });
                    Check(changed.StatusCode == HttpStatusCode.Redirect, "HTTP own contact update");
                    await using (var db = new MecaniCarContext(options)) {
                        var p = await db.Personas.SingleAsync(p => p.Email == Email("http"));
                        Check(p.Telefono == "1177777777" && p.Nombre == "Fixture" && p.Dni == data.Dni &&
                            (await db.Personas.FindAsync(googlePersona))!.Telefono == "1123456789", "HTTP mass assignment and IDOR blocked");
                    }
                    var denied = await client.GetAsync("/Usuario/Administracion");
                    Check(denied.StatusCode == HttpStatusCode.Forbidden || (denied.StatusCode == HttpStatusCode.Redirect &&
                        denied.Headers.Location!.ToString().Contains("AccesoDenegado")), "client cannot enter internal administration");
                    // Existing local CLIENTE with incomplete data exercises the same registration MVC
                    // screen as a Google pending account, without forging an OAuth cookie.
                    await using (var db = new MecaniCarContext(options)) {
                        var p = await db.Personas.SingleAsync(p => p.Email == Email("http"));
                        p.Telefono = null; await db.SaveChangesAsync();
                    }
                    var incomplete = await client.GetAsync("/PortalCliente/Index");
                    Check(incomplete.StatusCode == HttpStatusCode.Redirect && incomplete.Headers.Location!.ToString().Contains("RegistroCompletoCliente"),
                        "HTTP pending client redirected to complete registration");
                    var completeForm = await Page(client, "/RegistroCompletoCliente/Index");
                    Check(!Regex.IsMatch(completeForm, "<li[^>]+style=", RegexOptions.IgnoreCase), "registration summary respects CSP");
                    Check((await Post(client, "/RegistroCompletoCliente/Index", "", new())).StatusCode == HttpStatusCode.BadRequest,
                        "complete registration requires antiforgery");
                    var completed = await Post(client, "/RegistroCompletoCliente/Index", Anti(completeForm), new() {
                        ["Nombre"] = data.Nombre, ["Apellido"] = data.Apellido, ["Dni"] = data.Dni, ["Telefono"] = "1177777777",
                        ["Password"] = password, ["ConfirmarPassword"] = password, ["Email"] = "tampered@example.invalid" });
                    Check(completed.StatusCode == HttpStatusCode.Redirect &&
                        (completed.Headers.Location!.ToString() == "/" || completed.Headers.Location.ToString().Contains("Login")),
                        "registration HTTP completion and session revocation");
                    var revoked = await client.GetAsync("/PortalCliente/Index");
                    Check(revoked.StatusCode == HttpStatusCode.Redirect && revoked.Headers.Location!.ToString().Contains("Login"), "old registration cookie no longer grants access");
                    var renewed = await Post(client, "/Account/Login", Anti(await Page(client, "/Account/Login")),
                        new() { ["username"] = Email("http"), ["password"] = password });
                    Check(renewed.StatusCode == HttpStatusCode.Redirect && renewed.Headers.Location!.ToString().StartsWith("/PortalCliente"), "login after completion reaches portal");
                    var logout = await Post(client, "/Account/Logout", Anti(await Page(client, "/PortalCliente/Index")), new());
                    Check(logout.StatusCode == HttpStatusCode.Redirect, "logout POST works");
                    var afterLogout = await client.GetAsync("/PortalCliente/Index");
                    Check(afterLogout.StatusCode == HttpStatusCode.Redirect && afterLogout.Headers.Location!.ToString().Contains("Login"), "cookie removed after logout");
                    // Set up a Google-only fixture after obtaining a real authenticated cookie.
                    // No pre-existing account is involved; these writes use EF and preserve DVH/DVV.
                    int httpId;
                    await using (var db = new MecaniCarContext(options))
                        httpId = await db.Usuarios.Where(u => u.EmailLogin == Email("http")).Select(u => u.Id).SingleAsync();
                    Check((await Google(prefix + "httpGoogle", Email("http"), httpId)).Exitoso, "HTTP fixture links Google");
                    await Post(client, "/Account/Login", Anti(await Page(client, "/Account/Login")),
                        new() { ["username"] = Email("http"), ["password"] = password });
                    await using (var db = new MecaniCarContext(options)) {
                        var u = await db.Usuarios.SingleAsync(u => u.Id == httpId); u.PasswordHash = null; await db.SaveChangesAsync();
                    }
                    var onlyGooglePortal = await Page(client, "/PortalCliente/Index");
                    Check(WebUtility.HtmlDecode(onlyGooglePortal).Contains("Agregar contraseña") && !onlyGooglePortal.Contains("passwordGoogle"), "HTTP Google-only security actions");
                    Check((await Post(client, "/SeguridadCliente/SolicitarEnlace", "", new())).StatusCode == HttpStatusCode.BadRequest,
                        "credential proof request requires antiforgery");
                    var sent = await Post(client, "/SeguridadCliente/SolicitarEnlace", Anti(onlyGooglePortal), new());
                    Check(sent.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(smtp.PasswordToken), "HTTP password link sent to local SMTP fixture");
                    async Task<string> OpenLink(string token) {
                        using var request = new HttpRequestMessage(HttpMethod.Get, "/SeguridadCliente/Restablecer");
                        request.Headers.Add("X-Password-Token", token);
                        var response = await client.SendAsync(request);
                        Check(response.StatusCode == HttpStatusCode.OK && response.Headers.CacheControl?.NoStore == true &&
                            response.Headers.GetValues("Referrer-Policy").Single() == "no-referrer", "password link no-store and no-referrer");
                        var html = await response.Content.ReadAsStringAsync();
                        Check(html.Contains("ConfirmarPassword") && !html.Contains("Contrase&#xF1;aActual") && !html.Contains("<script"),
                            "password form contains no old password or third party resources");
                        return html;
                    }
                    var newForm = await OpenLink(smtp.PasswordToken);
                    Check(await Use<SeguridadClienteService, bool>(s => s.ValidarEnlaceAsync(smtp.PasswordToken)), "GET does not consume password link");
                    var values = new Dictionary<string, string> { ["Token"] = smtp.PasswordToken, ["Password"] = password, ["ConfirmarPassword"] = password,
                        ["UsuarioId"] = googleId.ToString() };
                    Check((await Post(client, "/SeguridadCliente/Restablecer", "", new(values))).StatusCode == HttpStatusCode.BadRequest,
                        "adding credential requires antiforgery");
                    var added = await Post(client, "/SeguridadCliente/Restablecer", Anti(newForm), values);
                    Check(added.StatusCode == HttpStatusCode.Redirect, "HTTP add password succeeds");
                    Check((await client.GetAsync("/PortalCliente/Index")).StatusCode == HttpStatusCode.Redirect, "add password revokes prior cookie");
                    var relogin = await Post(client, "/Account/Login", Anti(await Page(client, "/Account/Login")),
                        new() { ["username"] = Email("http"), ["password"] = password });
                    Check(relogin.StatusCode == HttpStatusCode.Redirect, "new local credential authenticates same account");
                    var bothPortal = await Page(client, "/PortalCliente/Index");
                    Check(!WebUtility.HtmlDecode(bothPortal).Contains("Agregar contraseña") && !bothPortal.Contains("passwordGoogle") &&
                        bothPortal.Contains("Cambiar"), "HTTP both methods show change password only");
                    await Post(client, "/SeguridadCliente/SolicitarEnlace", Anti(bothPortal), new());
                    var changeForm = await OpenLink(smtp.PasswordToken);
                    var changedPassword = password + "http";
                    Check((await Post(client, "/SeguridadCliente/Restablecer", Anti(changeForm), new() {
                        ["Token"] = smtp.PasswordToken, ["Password"] = changedPassword, ["ConfirmarPassword"] = changedPassword })).StatusCode == HttpStatusCode.Redirect,
                        "HTTP password change by email link without old password");
                    Check(!(await Use<AccountService, LoginResult>(s => s.LoginAsync(Email("http"), password))).Exitoso,
                        "old password no longer authenticates");
                    var recoveryForm = await Page(client, "/SeguridadCliente/Recuperar");
                    var recovered = await Post(client, "/SeguridadCliente/Recuperar", Anti(recoveryForm), new() { ["Email"] = Email("http") });
                    var neutral = WebUtility.HtmlDecode(await recovered.Content.ReadAsStringAsync());
                    var recoveryToken = smtp.PasswordToken;
                    var absent = await Post(client, "/SeguridadCliente/Recuperar", Anti(await Page(client, "/SeguridadCliente/Recuperar")),
                        new() { ["Email"] = Email("absent") });
                    Check(recovered.StatusCode == absent.StatusCode && neutral.Contains(SeguridadClienteService.RespuestaNeutra) &&
                        WebUtility.HtmlDecode(await absent.Content.ReadAsStringAsync()).Contains(SeguridadClienteService.RespuestaNeutra) &&
                        smtp.PasswordToken == recoveryToken, "recovery response neutral for registered and absent account");
                    var resetForm = await OpenLink(recoveryToken);
                    Check((await Post(client, "/SeguridadCliente/Restablecer", Anti(resetForm), new() {
                        ["Token"] = recoveryToken, ["Password"] = password, ["ConfirmarPassword"] = password })).StatusCode == HttpStatusCode.Redirect,
                        "HTTP recovery from login succeeds");
                    Check((await Use<AccountService, LoginResult>(s => s.LoginAsync(Email("http"), password))).Usuario?.Id == httpId &&
                        (await Google(prefix + "httpGoogle", Email("http"))).Data?.FindFirst(ClaimTypes.NameIdentifier)?.Value == httpId.ToString(),
                        "recovery preserves same user and Google identity");
                    using var cajaClient = Client();
                    var cajaLogin = await Post(cajaClient, "/Account/Login", Anti(await Page(cajaClient, "/Account/Login")),
                        new() { ["username"] = Email("caja"), ["password"] = password });
                    Check(cajaLogin.StatusCode == HttpStatusCode.Redirect && !cajaLogin.Headers.Location!.ToString().Contains("RegistroCompleto"), "CAJA internal login unaffected");
                    await Page(cajaClient, cajaLogin.Headers.Location!.ToString());
                    foreach (var role in new[] { "ADMIN", "MECANICO", "STOCK" }) {
                        await using (var db = new MecaniCarContext(options)) {
                            var internalUser = new Usuario { Username = prefix + role, EmailLogin = Email(role), PrimerLogin = false,
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Persona = new Persona { Nombre = "Fixture", Apellido = role, Email = Email(role) } };
                            internalUser.Persona.Roles.Add(new PersonaRol { RolId = await db.Roles.Where(r => r.Nombre == role && r.Activo).Select(r => r.Id).SingleAsync() });
                            db.Usuarios.Add(internalUser); await db.SaveChangesAsync();
                        }
                        using var internalClient = Client();
                        var internalLogin = await Post(internalClient, "/Account/Login", Anti(await Page(internalClient, "/Account/Login")),
                            new() { ["username"] = Email(role), ["password"] = password });
                        Check(internalLogin.StatusCode == HttpStatusCode.Redirect && !internalLogin.Headers.Location!.ToString().Contains("RegistroCompleto"),
                            role + " login unaffected");
                        await Page(internalClient, internalLogin.Headers.Location!.ToString());
                    }
                }
                finally
                {
                    if (!app.HasExited) {
                        // Stop only the process created by this test; never other user processes.
                        app.Kill(); await app.WaitForExitAsync();
                    }
                }
            }
        }
        finally
        {
            // Restrict cleanup to this run's exact email prefix and relational IDs.
            // EF maintains DVH/DVV; no ExecuteDelete / manual hash updates / baseline reset.
            await using var db = new MecaniCarContext(options);
            var people = await db.Personas.Where(p => p.Email != null && p.Email.StartsWith(prefix)).ToListAsync();
            var personIds = people.Select(p => p.Id).ToArray();
            var users = await db.Usuarios.Where(u => personIds.Contains(u.PersonaId)).ToListAsync();
            var userIds = users.Select(u => u.Id).ToArray();
            var invitations = await db.InvitacionesCliente.Where(i => personIds.Contains(i.PersonaId)).ToListAsync();
            var invitationIds = invitations.Select(i => i.Id).ToArray();
            await using var tx = await db.Database.BeginTransactionAsync();
            db.Auditorias.RemoveRange(await db.Auditorias.Where(a => (a.UsuarioId != null && userIds.Contains(a.UsuarioId.Value)) ||
                (a.Entidad == "Usuario" && a.EntidadId != null && userIds.Contains(a.EntidadId.Value)) ||
                (a.Entidad == "Persona" && a.EntidadId != null && personIds.Contains(a.EntidadId.Value)) ||
                (a.Entidad == "InvitacionCliente" && a.EntidadId != null && invitationIds.Contains(a.EntidadId.Value))).ToListAsync());
            db.IdentidadesExternas.RemoveRange(await db.IdentidadesExternas.Where(i => userIds.Contains(i.UsuarioId)).ToListAsync());
            db.EnlacesPasswordCliente.RemoveRange(await db.EnlacesPasswordCliente.Where(e => userIds.Contains(e.UsuarioId)).ToListAsync());
            db.InvitacionesCliente.RemoveRange(invitations);
            db.PersonaRoles.RemoveRange(await db.PersonaRoles.Where(p => personIds.Contains(p.PersonaId)).ToListAsync());
            await db.SaveChangesAsync();
            db.Usuarios.RemoveRange(users); await db.SaveChangesAsync();
            db.Personas.RemoveRange(people); await db.SaveChangesAsync(); await tx.CommitAsync();
            Check(!await db.Personas.AnyAsync(p => personIds.Contains(p.Id)) && !await db.Usuarios.AnyAsync(u => userIds.Contains(u.Id)) &&
                !await db.InvitacionesCliente.AnyAsync(i => invitationIds.Contains(i.Id)), "fixtures cleaned through EF");
            await Integrity("after cleanup");
        }
    }

    private sealed class CapturedMail(IConfiguration config) : EmailService(config)
    {
        public System.Collections.Concurrent.ConcurrentDictionary<string, string> Tokens { get; } = new(StringComparer.OrdinalIgnoreCase);
        public System.Collections.Concurrent.ConcurrentDictionary<string, string> Codes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public System.Collections.Concurrent.ConcurrentDictionary<string, string> PasswordTokens { get; } = new(StringComparer.OrdinalIgnoreCase);
        public System.Collections.Concurrent.ConcurrentBag<string> PasswordHistory { get; } = new();
        protected override Task EnviarMensajeAsync(SmtpClient smtp, MailMessage mail)
        {
            var token = Regex.Match(mail.Body, "Index#([^\"]+)");
            if (token.Success) Tokens[mail.To.Single().Address] = WebUtility.HtmlDecode(token.Groups[1].Value);
            var code = Regex.Match(mail.Body, "<code>([^<]+)</code>");
            if (code.Success) Codes[mail.To.Single().Address] = WebUtility.HtmlDecode(code.Groups[1].Value);
            var passwordToken = Regex.Match(mail.Body, "Restablecer#([^\"]+)");
            if (passwordToken.Success) {
                var value = WebUtility.HtmlDecode(passwordToken.Groups[1].Value);
                PasswordTokens[mail.To.Single().Address] = value; PasswordHistory.Add(value);
            }
            return Task.CompletedTask;
        }
    }
    // Loopback-only SMTP capture for the real HTTP process; no external delivery or logging.
    private sealed class LocalMail : IDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new();
        private readonly Task loop;
        public int Port { get; }
        public string Code = "";
        public string PasswordToken = "";
        public LocalMail()
        {
            listener.Start(); Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            loop = Task.Run(async () => {
                try {
                    while (!stop.IsCancellationRequested) {
                        using var socket = await listener.AcceptTcpClientAsync(stop.Token);
                        using var reader = new StreamReader(socket.GetStream());
                        using var writer = new StreamWriter(socket.GetStream()) { AutoFlush = true, NewLine = "\r\n" };
                        await writer.WriteLineAsync("220 localhost test");
                        while (await reader.ReadLineAsync(stop.Token) is string line) {
                            if (line.StartsWith("QUIT")) { await writer.WriteLineAsync("221 bye"); break; }
                            if (line.StartsWith("DATA")) {
                                await writer.WriteLineAsync("354 data");
                                var data = new System.Text.StringBuilder();
                                while (await reader.ReadLineAsync(stop.Token) is string part && part != ".") data.Append(part).Append("\r\n");
                                var raw = data.ToString(); var split = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                                var body = split >= 0 ? raw[(split + 4)..] : raw;
                                if (raw[..Math.Max(0, split)].Contains("base64", StringComparison.OrdinalIgnoreCase))
                                    body = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(body));
                                else {
                                    body = body.Replace("=\r\n", "");
                                    body = Regex.Replace(body, "=([0-9A-Fa-f]{2})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
                                }
                                var match = Regex.Match(body, "<code>([^<]+)</code>");
                                if (match.Success) Code = WebUtility.HtmlDecode(match.Groups[1].Value);
                                var passwordToken = Regex.Match(body, "Restablecer#([^\"]+)");
                                if (passwordToken.Success) PasswordToken = WebUtility.HtmlDecode(passwordToken.Groups[1].Value);
                                await writer.WriteLineAsync("250 accepted");
                            }
                            else await writer.WriteLineAsync("250 OK");
                        }
                    }
                } catch (Exception ex) when (ex is OperationCanceledException or SocketException or ObjectDisposedException) { }
            });
        }
        public void Dispose() { stop.Cancel(); listener.Stop(); loop.GetAwaiter().GetResult(); stop.Dispose(); }
    }
    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "MecaniCar360";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
