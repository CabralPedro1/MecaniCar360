using System.Net;
using System.Net.Mail;
using System.Reflection;
using MecaniCar360.Controllers;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MecaniCar360.Tests;

public class EmisionCorreoCuentaTests
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(59, false)]
    [InlineData(60, true)]
    [InlineData(61, true)]
    public void Intervalo_IncluyeLimiteExacto(int segundos, bool permitido)
    {
        var ultima = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(permitido, EmisionCorreoCuenta.PuedeReemitir(ultima, ultima.AddSeconds(segundos)));
        Assert.True(EmisionCorreoCuenta.PuedeReemitir(null, ultima));
    }

    [Fact]
    public async Task Orientacion_ReservaAtomicaYRecuperacionTrasIntervalo()
    {
        var limite = new LimiteOrientacionCorreo(); var ahora = DateTime.UtcNow;
        var resultados = await Task.WhenAll(Enumerable.Range(0, 30)
            .Select(_ => Task.Run(() => limite.IntentarReservar(1, ahora))));
        Assert.Single(resultados, x => x);
        Assert.True(limite.IntentarReservar(2, ahora));
        Assert.False(limite.IntentarReservar(1, ahora.AddSeconds(59)));
        Assert.True(limite.IntentarReservar(1, ahora.AddSeconds(60)));
        Assert.False(limite.IntentarReservar(0, ahora));
    }

    [Fact]
    public void Orientacion_CapacidadAcotadaSinExpulsarLimitesVigentes()
    {
        var limite = new LimiteOrientacionCorreo(); var ahora = DateTime.UtcNow;
        for (var i = 1; i <= 10000; i++) Assert.True(limite.IntentarReservar(i, ahora));
        Assert.False(limite.IntentarReservar(10001, ahora));
        Assert.False(limite.IntentarReservar(1, ahora));
        Assert.True(limite.IntentarReservar(10001, ahora.AddSeconds(60)));
    }

    [Theory]
    [InlineData(typeof(SeguridadClienteController), "Recuperar")]
    [InlineData(typeof(SeguridadClienteController), "SolicitarEnlace")]
    [InlineData(typeof(RegistroClienteController), "Index")]
    [InlineData(typeof(InvitacionClienteController), "Emitir")]
    public void Emisores_ConservanAntiforgeryYPoliticaEspecifica(Type controller, string accion)
    {
        var metodo = controller.GetMethods().Single(m => m.Name == accion && m.IsDefined(typeof(HttpPostAttribute)));
        Assert.NotNull(metodo.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Equal(EmisionCorreoCuenta.PoliticaHttp, metodo.GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName);
        Assert.NotEqual(Program.LoginRateLimitPolicy, EmisionCorreoCuenta.PoliticaHttp);
    }

    [Fact]
    public void Persistencia_ControlAntesDeGeneracionOInvalidacion()
    {
        // Inspección estructural complementaria: no demuestra rollback o concurrencia SQL.
        var seguridad = CalificacionEntregaTests.Fuente("Services/SeguridadClienteService.cs");
        var inicio = seguridad.IndexOf("private async Task<bool> EmitirEnlaceAsync");
        var fin = seguridad.IndexOf("private async Task<Usuario?> ValidarCoreAsync", inicio);
        var metodo = seguridad[inicio..fin];
        Assert.True(metodo.IndexOf("await BloquearPersonaAsync") < metodo.IndexOf("EmisionCorreoCuenta.PuedeReemitir"));
        Assert.True(metodo.IndexOf("await db.Entry(enlace).ReloadAsync") < metodo.IndexOf("EmisionCorreoCuenta.PuedeReemitir"));
        Assert.True(metodo.IndexOf("EmisionCorreoCuenta.PuedeReemitir") < metodo.IndexOf("TokenInvitacion.Generar()"));
        Assert.True(metodo.IndexOf("TokenInvitacion.Generar()") < metodo.IndexOf("enlace.TokenHash = hash"));
        Assert.Contains("e.TokenHash == hash && e.FechaConsumida == null", metodo);
        var invitacion = CalificacionEntregaTests.Fuente("Services/InvitacionClienteService.cs");
        inicio = invitacion.IndexOf("private async Task<ServiceResult> EmitirCoreAsync");
        metodo = invitacion[inicio..];
        Assert.True(metodo.IndexOf("await BloquearPersonaAsync") < metodo.IndexOf("EmisionCorreoCuenta.PuedeReemitir"));
        Assert.True(metodo.IndexOf("EmisionCorreoCuenta.PuedeReemitir") < metodo.IndexOf("TokenInvitacion.Generar()"));
        Assert.True(metodo.IndexOf("TokenInvitacion.Generar()") < metodo.IndexOf("await InvalidarAsync"));
        Assert.Contains(".MaxAsync(i => (DateTime?)i.FechaCreacion)", metodo);
        Assert.Contains("SeguridadClienteService.RespuestaNeutra", CalificacionEntregaTests.Fuente("Views/SeguridadCliente/Solicitada.cshtml"));
    }

    [Fact]
    public async Task RecuperacionEntradaNoElegible_RespuestaNeutraSinDependencias()
    {
        var seguridad = new SeguridadClienteService(null!, null!, null!, null!, null!, null!, null!, null!, null!);
        var controller = new SeguridadClienteController(seguridad, null!);
        var resultado = Assert.IsType<ViewResult>(await controller.Recuperar(
            new MecaniCar360.Models.ViewModels.RecuperarPasswordClienteViewModel { Email = "invalido" }));
        Assert.Equal("Solicitada", resultado.ViewName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Orientacion_TransporteSimuladoYFalloNoLiberanReserva(bool fallar)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp.invalid", ["Smtp:Port"] = "587",
            ["Smtp:User"] = "prueba@example.invalid", ["Smtp:Password"] = "fixture-no-real"
        }).Build();
        var correo = new CorreoSimulado(config, fallar);
        var seguridad = new SeguridadClienteService(null!, null!, null!, new EphemeralDataProtectionProvider(),
            correo, null!, NullLogger<SeguridadClienteService>.Instance, config, null!);
        var limite = new LimiteOrientacionCorreo(); var ahora = DateTime.UtcNow;
        Assert.True(limite.IntentarReservar(1, ahora));
        var enviar = typeof(SeguridadClienteService).GetMethod("EnviarAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var resultado = await (Task<bool>)enviar.Invoke(seguridad,
            new object[] { new Usuario { Id = 1, EmailLogin = "destino@example.invalid", SecurityStamp = "fixture" }, "orientacion" })!;
        Assert.Equal(!fallar, resultado);
        Assert.Equal(1, correo.Intentos);
        Assert.False(limite.IntentarReservar(1, ahora.AddSeconds(59)));
        Assert.True(limite.IntentarReservar(1, ahora.AddSeconds(60)));
    }

    private sealed class CorreoSimulado(IConfiguration config, bool fallar) : EmailService(config)
    {
        public int Intentos { get; private set; }
        protected override Task EnviarMensajeAsync(SmtpClient smtp, MailMessage mail)
        {
            Intentos++;
            Assert.Equal("destino@example.invalid", mail.To.Single().Address);
            return fallar ? Task.FromException(new SmtpException("Fallo simulado")) : Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Http_CupoCompartidoPorIp_AntiforgeryYLoginIndependiente()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(o => o.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF");
        builder.Services.AddRateLimiter(o =>
        {
            EmisionCorreoCuenta.ConfigurarLimiteHttp(o);
            o.RejectionStatusCode = 429;
            o.OnRejected = (c, _) =>
            {
                if (c.Lease.TryGetMetadata(System.Threading.RateLimiting.MetadataName.RetryAfter, out var espera))
                    c.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(espera.TotalSeconds).ToString();
                return ValueTask.CompletedTask;
            };
            o.AddFixedWindowLimiter(Program.LoginRateLimitPolicy, l =>
            { l.PermitLimit = 20; l.Window = TimeSpan.FromMinutes(1); l.QueueLimit = 0; });
        });
        await using var app = builder.Build();
        // Sólo en este host de pruebas: simula distintas conexiones sin confiar en headers en producción.
        app.Use(async (ctx, next) =>
        { ctx.Connection.RemoteIpAddress = IPAddress.Parse(ctx.Request.Headers["X-Test-IP"].FirstOrDefault() ?? "192.0.2.1"); await next(ctx); });
        app.UseRouting(); app.UseRateLimiter();
        var envios = 0;
        app.MapGet("/csrf", (HttpContext c, IAntiforgery a) => a.GetAndStoreTokens(c).RequestToken!);
        foreach (var ruta in new[] { "/recuperar", "/solicitar", "/invitacion" })
            app.MapPost(ruta, async (HttpContext c, IAntiforgery a) =>
            {
                try { await a.ValidateRequestAsync(c); }
                catch (AntiforgeryValidationException) { c.Response.StatusCode = 400; return; }
                Interlocked.Increment(ref envios);
                await c.Response.WriteAsync(SeguridadClienteService.RespuestaNeutra);
            }).RequireRateLimiting(EmisionCorreoCuenta.PoliticaHttp);
        app.MapPost("/login", () => "login").RequireRateLimiting(Program.LoginRateLimitPolicy);
        await app.StartAsync();
        try
        {
            using var client = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer() });
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var token = await client.GetStringAsync(address + "/csrf");
            client.DefaultRequestHeaders.Add("X-Test-IP", "192.0.2.2");
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(address + "/recuperar", null)).StatusCode);
            Assert.Equal(0, envios);
            client.DefaultRequestHeaders.Remove("X-Test-IP");
            client.DefaultRequestHeaders.Add("X-CSRF", token);
            for (var i = 0; i < 5; i++)
            {
                var response = await client.PostAsync(address + (i % 2 == 0 ? "/recuperar" : "/solicitar"), null);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(SeguridadClienteService.RespuestaNeutra, await response.Content.ReadAsStringAsync());
            }
            client.DefaultRequestHeaders.Add("X-Test-IP", "::ffff:192.0.2.1");
            var rechazado = await client.PostAsync(address + "/invitacion", null);
            Assert.Equal(HttpStatusCode.TooManyRequests, rechazado.StatusCode);
            Assert.NotNull(rechazado.Headers.RetryAfter);
            Assert.Equal(5, envios);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(address + "/login", null)).StatusCode);
            client.DefaultRequestHeaders.Remove("X-Test-IP");
            client.DefaultRequestHeaders.Add("X-Test-IP", "192.0.2.3");
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(address + "/invitacion", null)).StatusCode);
            Assert.Equal(6, envios);
        }
        finally { await app.StopAsync(); }
    }
}
