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
                if (path == "/token") Assert.Equal("default-src 'self'; style-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single());
            }
            using var script = await client.GetAsync("/script.js");
            Assert.True(script.Headers.CacheControl!.Public);
            Assert.False(script.Headers.Contains("Referrer-Policy"));
            using var oauth = await client.GetAsync("/oauth");
            Assert.Equal(HttpStatusCode.Redirect, oauth.StatusCode);
            Assert.Equal("https://accounts.google.com/", oauth.Headers.Location!.AbsoluteUri);
        }
        finally { await app.StopAsync(); }
    }
}
