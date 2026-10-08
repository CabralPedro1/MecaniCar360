using System.Reflection;
using MecaniCar360.Data;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MecaniCar360.Tests;

public sealed class InvitacionConfiguracionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("http://example.invalid")]
    [InlineData("http://localhost:5190/#dato-que-no-debe-loguearse")]
    public async Task UrlAusenteOInvalida_NoConsultaSqlNiEnvia_YRegistraSoloDiagnosticoSeguro(string? url)
    {
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer("Server=no-server.invalid;Database=NoAccess;Integrated Security=True;Connect Timeout=1").Options);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Invitaciones:UrlBase"] = url }).Build();
        var logger = new Capture();
        // Dependencies after the URL precondition must never be used.
        var service = new InvitacionClienteService(db, null!, null!, null!, config,
            new EnvironmentStub(), logger, null!);
        var result = await (Task<ServiceResult>)typeof(InvitacionClienteService)
            .GetMethod("EmitirPublicaAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, new object[] { 1 })!;
        Assert.False(result.Exitoso);
        Assert.Empty(db.ChangeTracker.Entries());
        var log = Assert.Single(logger.Events);
        Assert.Equal(4100, log.Id.Id);
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Null(log.Exception);
        Assert.Contains("Invitaciones:UrlBase", log.Text);
        if (!string.IsNullOrEmpty(url)) Assert.DoesNotContain(url, log.Text);
    }

    private sealed class Capture : ILogger<InvitacionClienteService>
    {
        public List<(EventId Id, LogLevel Level, string Text, Exception? Exception)> Events { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Events.Add((id, level, formatter(state, exception), exception));
    }
    private sealed class EnvironmentStub : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "MecaniCar360";
        public string ContentRootPath { get; set; } = "";
        public string WebRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
