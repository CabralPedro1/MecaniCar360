using System.Net;
using MecaniCar360.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MecaniCar360.Tests;

public class CabecerasHtmlTests
{
    [Fact]
    public async Task Http_ErroresYCspExistente_SinDuplicaciones()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        await using var app = builder.Build();
        app.UseMiddleware<CabecerasHtmlMiddleware>();
        app.UseExceptionHandler("/error");
        // Deliberately register a second callback, including on error reexecution.
        app.UseMiddleware<CabecerasHtmlMiddleware>();
        const string existing = "default-src 'self'; script-src 'self'; frame-ancestors 'none'; base-uri 'none'";
        app.Run(async context =>
        {
            if (context.Request.Path == "/throw") throw new InvalidOperationException("Synthetic test failure");
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            if (context.Request.Path == "/existing") context.Response.Headers["Content-Security-Policy"] = existing;
            else if (context.Request.Path == "/self") context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'; style-src 'self'";
            else if (context.Request.Path != "/error") context.Response.StatusCode = int.Parse(context.Request.Path.Value!.Trim('/'));
            await context.Response.WriteAsync("<p>Respuesta de prueba</p>");
        });
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            foreach (var status in new[] { 200, 400, 403, 404, 500, 503 })
            {
                using var response = await client.GetAsync("/" + status);
                Assert.Equal(status, (int)response.StatusCode);
                Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
                Assert.Equal("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
                Assert.Equal("<p>Respuesta de prueba</p>", await response.Content.ReadAsStringAsync());
            }
            foreach (var path in new[] { "/existing", "/self", "/throw" })
            {
                using var response = await client.GetAsync(path);
                Assert.Equal(path == "/throw" ? HttpStatusCode.InternalServerError : HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
                Assert.Equal(path == "/existing" ? existing : path == "/self"
                    ? "frame-ancestors 'self'; style-src 'self', frame-ancestors 'none'" : "frame-ancestors 'none'",
                    response.Headers.GetValues("Content-Security-Policy").Single());
            }
        }
        finally { await app.StopAsync(); }
    }

    [Fact]
    public async Task Http_Integridad503_ProtegidaSinEjecutarEndpoint()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(new MecaniCar360.Services.EstadoIntegridad(
            Path.Combine(Path.GetTempPath(), "sec04-private"), Path.Combine(Path.GetTempPath(), "sec04-public")));
        await using var app = builder.Build();
        app.UseMiddleware<CabecerasHtmlMiddleware>();
        app.UseMiddleware<IntegridadMiddleware>();
        app.Run(_ => throw new InvalidOperationException("Endpoint must not execute"));
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient();
            using var response = await client.GetAsync(address);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
            Assert.Equal("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
            Assert.Contains("Sistema no disponible", await response.Content.ReadAsStringAsync());
        }
        finally { await app.StopAsync(); }
    }

    [Fact]
    public void Pipeline_CabecerasAntesDeErroresEIntegridad()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "MecaniCar360.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        var source = File.ReadAllText(Path.Combine(root.FullName, "Program.cs"));
        var header = source.IndexOf("app.UseMiddleware<MecaniCar360.Middleware.CabecerasHtmlMiddleware>();", StringComparison.Ordinal);
        Assert.True(header >= 0);
        Assert.True(source.IndexOf("app.UseExceptionHandler(", StringComparison.Ordinal) > header);
        Assert.True(source.IndexOf("app.UseMiddleware<MecaniCar360.Middleware.IntegridadMiddleware>();", StringComparison.Ordinal) > header);
    }

    [Fact]
    public async Task Http_HtmlProtegidoSinCambiarCspNiRedirectOAuthNiRecursos()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        await using var app = builder.Build();
        app.UseMiddleware<CabecerasHtmlMiddleware>();
        app.Run(async context =>
        {
            if (context.Request.Path == "/oauth")
            {
                context.Response.Redirect("https://accounts.google.com/");
                return;
            }
            if (context.Request.Path == "/script.js")
            {
                context.Response.ContentType = "text/javascript";
                context.Response.Headers.CacheControl = "public, max-age=60";
                await context.Response.WriteAsync("void 0;");
                return;
            }
            if (context.Request.Path == "/token")
            {
                context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self'";
                context.Response.Headers["Referrer-Policy"] = "no-referrer";
            }
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync("<form method='post'>Formulario</form>");
        });
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(address) };
            foreach (var path in new[] { "/login", "/privada", "/token" })
            {
                using var response = await client.GetAsync(path);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("<form method='post'>Formulario</form>", await response.Content.ReadAsStringAsync());
                Assert.True(response.Headers.CacheControl!.NoStore);
                Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
                Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
                Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
                Assert.Equal(path == "/token" ? "default-src 'self'; style-src 'self', frame-ancestors 'none'" : "frame-ancestors 'none'",
                    response.Headers.GetValues("Content-Security-Policy").Single());
            }
            using var script = await client.GetAsync("/script.js");
            Assert.True(script.Headers.CacheControl!.Public);
            Assert.False(script.Headers.Contains("Referrer-Policy"));
            Assert.False(script.Headers.Contains("X-Frame-Options"));
            Assert.False(script.Headers.Contains("Content-Security-Policy"));
            using var oauth = await client.GetAsync("/oauth");
            Assert.Equal(HttpStatusCode.Redirect, oauth.StatusCode);
            Assert.Equal("https://accounts.google.com/", oauth.Headers.Location!.AbsoluteUri);
            Assert.False(oauth.Headers.Contains("X-Frame-Options"));
        }
        finally { await app.StopAsync(); }
    }
}
