using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace MecaniCar360.Concurrency;

// Count warnings/errors without retaining formatted messages or exception details.
internal sealed class AvisosCorreoPrueba : ILoggerProvider
{
    private int count;
    public int Cantidad => Volatile.Read(ref count);
    public ILogger CreateLogger(string categoryName) => new Counter(this, categoryName);
    public void Dispose() { }
    private sealed class Counter(AvisosCorreoPrueba owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Warning &&
            (category == typeof(SeguridadClienteService).FullName || category == typeof(InvitacionClienteService).FullName);
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        { if (IsEnabled(level)) Interlocked.Increment(ref owner.count); }
    }
}

// One instance per ConcurrencyEnvironment/scenario, shared only by its participants.
// Never calls SmtpClient.Send; no logging of body, destination, password or token.
internal sealed class CorreoCuentaSimulado() : EmailService(new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string,string?> {
        ["Smtp:Host"]="smtp.example.invalid", ["Smtp:Port"]="1",
        ["Smtp:User"]="fixture@example.invalid", ["Smtp:Password"]="unused-fixture",
        ["Smtp:EnableSsl"]="false"
    }).Build())
{
    private readonly ConcurrentQueue<string> tokens = new();
    private int attempts;
    public int Intentos => Volatile.Read(ref attempts);
    public bool Fallar { get; set; }
    public string UltimoToken => tokens.Last();
    protected override Task EnviarMensajeAsync(SmtpClient smtp, MailMessage mail)
    {
        var match = Regex.Match(mail.Body, "(?:Restablecer|Index)#([^\"]+)");
        if (!match.Success) throw new InvalidOperationException("Unexpected fixture mail format.");
        tokens.Enqueue(WebUtility.HtmlDecode(match.Groups[1].Value));
        Interlocked.Increment(ref attempts);
        if (Fallar) throw new SmtpException("Simulated transport failure.");
        return Task.CompletedTask;
    }
}

internal sealed class AmbienteCorreoPrueba : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "MecaniCar360";
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = "";
    public string WebRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
