using System.Data.Common;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.ExceptionServices;
using MecaniCar360.Controllers;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MecaniCar360.Tests;

public class ActivacionAdminTests
{
    private static readonly DateTime Ahora = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);
    private const string Email = "admin@example.invalid";
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value)).Build();
    private static object? Call(string type, string method, params object?[] args)
    {
        try { return typeof(Program).Assembly.GetType("MecaniCar360.Helpers." + type)!
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); throw; }
    }
    private static Usuario UsuarioPendiente() => new()
    {
        Id = 12, Username = "admin", EmailLogin = Email, Activo = false, PrimerLogin = true,
        PasswordHash = null, SecurityStamp = Guid.NewGuid().ToString("N"), Persona = new Persona
        {
            Activo = true, Roles = new List<PersonaRol> { new() { Rol = new Rol { Nombre = RolesSistema.ADMIN, Activo = true } } }
        }
    };
    private static EnlacePasswordCliente Marca(Usuario u) =>
        (EnlacePasswordCliente)Call("ActivacionAdminInicial", "CrearMarca", u, Ahora)!;
    private static bool Pendiente(Usuario u, EnlacePasswordCliente? e, string email = Email) =>
        (bool)Call("ActivacionAdminInicial", "EsPendiente", u, e, email)!;
    private static bool Vigente(Usuario u, EnlacePasswordCliente e, DateTime now) =>
        (bool)Call("ActivacionAdminInicial", "EsVigente", u, e, Email, now)!;

    [Fact]
    public void MarcaInicialNoEsUnEnlaceUtilizable_NiUnaCuentaAutenticable()
    {
        var u = UsuarioPendiente(); var e = Marca(u);
        Assert.Null(u.PasswordHash); Assert.False(u.Activo); Assert.True(u.PrimerLogin);
        Assert.True(Pendiente(u, e)); Assert.False(Vigente(u, e, Ahora));
        Assert.True((bool)Call("ActivacionAdminInicial", "EsMarcaSinEmision", u, e)!);
        Assert.Equal(FinalidadPasswordCliente.Agregar, e.Finalidad);
        Assert.Equal(64, e.TokenHash.Length); Assert.Equal(64, e.StampHash.Length); Assert.Equal(64, e.EmailHash.Length);
        Assert.Equal(Ahora.AddMinutes(10), e.FechaExpiracion);
        Assert.NotEqual(TokenInvitacion.Hash(u.SecurityStamp), e.StampHash);
    }

    [Theory]
    [InlineData("activo")] [InlineData("password")] [InlineData("primerLogin")]
    [InlineData("persona")] [InlineData("rol")] [InlineData("rolInactivo")]
    [InlineData("rolBaja")] [InlineData("cliente")]
    [InlineData("stamp")] [InlineData("email")] [InlineData("config")]
    [InlineData("consumido")] [InlineData("finalidad")] [InlineData("teniaPassword")]
    [InlineData("sinMarca")] [InlineData("otroUsuario")]
    public void SoloBootstrapIdentificadoPuedeReemitirse(string cambio)
    {
        var u = UsuarioPendiente(); EnlacePasswordCliente? e = Marca(u); var email = Email;
        switch (cambio)
        {
            case "activo": u.Activo = true; break;
            case "password": u.PasswordHash = "hash-legado-no-es-una-credencial"; break;
            case "primerLogin": u.PrimerLogin = false; break;
            case "persona": u.Persona.Activo = false; break;
            case "rol": u.Persona.Roles.Clear(); break;
            case "rolInactivo": u.Persona.Roles[0].Rol.Activo = false; break;
            case "rolBaja": u.Persona.Roles[0].FechaBaja = Ahora; break;
            case "cliente": u.Persona.Roles.Add(new() { Rol = new Rol { Nombre = RolesSistema.CLIENTE, Activo = true } }); break;
            case "stamp": u.SecurityStamp = Guid.NewGuid().ToString("N"); break;
            case "email": u.EmailLogin = "otro@example.invalid"; break;
            case "config": email = "otro@example.invalid"; break;
            case "consumido": e.FechaConsumida = Ahora; break;
            case "finalidad": e.Finalidad = FinalidadPasswordCliente.Recuperar; break;
            case "teniaPassword": e.TeniaPassword = true; break;
            case "sinMarca": e = null; break;
            case "otroUsuario": e.UsuarioId++; break;
        }
        Assert.False(Pendiente(u, e, email));
    }

    [Fact]
    public void TokensDeClienteYAdminTienenDominiosDistintos()
    {
        var token = TokenInvitacion.Generar();
        Assert.True(TokenInvitacion.FormatoValido(token));
        Assert.Equal(32, Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(token).Length);
        var admin = (string)Call("ActivacionAdminInicial", "HashToken", token)!;
        Assert.True(admin != TokenInvitacion.Hash(token));
        Assert.True(admin != (string)Call("ActivacionAdminInicial", "HashToken", TokenInvitacion.Generar())!);
    }

    [Fact]
    public void VencimientoInvalidacionYConsumoNoImpidenReenvioLegitimoExceptoConsumo()
    {
        var u = UsuarioPendiente(); var e = Marca(u); e.FechaInvalidacion = null;
        Assert.True(Vigente(u, e, Ahora.AddMinutes(9)));
        Assert.False(Vigente(u, e, Ahora.AddMinutes(10)));
        Assert.False(Vigente(u, e, Ahora.AddSeconds(-1)));
        e.FechaInvalidacion = Ahora;
        Assert.False(Vigente(u, e, Ahora)); Assert.True(Pendiente(u, e));
        e.FechaConsumida = Ahora;
        Assert.False(Vigente(u, e, Ahora)); Assert.False(Pendiente(u, e));
    }

    [Fact]
    public void DestinatarioEsExplicito_NoSeDeduceDeSmtp()
    {
        Assert.Throws<InvalidOperationException>(() => Call("ActivacionAdminInicial", "EmailConfigurado", Config(("Smtp:User", Email))));
        Assert.Equal(Email, Call("ActivacionAdminInicial", "EmailConfigurado", Config(("BootstrapAdmin:Email", " " + Email + " "), ("Smtp:User", "otro@example.invalid"))));
    }

    [Theory]
    [InlineData(null, "S-1-5-21-1", true, false)]
    [InlineData("S-1-5-21-1", null, true, false)]
    [InlineData("S-1-5-21-1", "S-1-5-21-2", true, false)]
    [InlineData("S-1-5-21-1", "S-1-5-21-1", false, false)]
    [InlineData("S-1-5-21-1", "S-1-5-21-1", true, true)]
    public void OperadorExigeSidExplicitoCoincidenteYElevacion(string? actual, string? configured, bool elevated, bool expected) =>
        Assert.Equal(expected, Call("BootstrapAdminLocal", "OperadorPermitido", actual, configured, elevated));

    private sealed class NoSql : DbConnectionInterceptor
    {
        public int Attempts;
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        { Attempts++; throw new InvalidOperationException("SQL prohibited."); }
    }
    [Theory]
    [InlineData("larga")] [InlineData("diferente")] [InlineData("telefono")] [InlineData("token")]
    public async Task DatosInvalidosNoConsumenEnlaceNiAbrenSql(string caso)
    {
        var guard = new NoSql();
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer("Server=not-used.invalid;Database=NeverOpened;Integrated Security=True").AddInterceptors(guard).Options);
        var vm = new ActivarAdminViewModel { Token = TokenInvitacion.Generar(), Password = "Aa1!" + new string('x', 69) };
        vm.ConfirmarPassword = vm.Password;
        if (caso == "diferente") vm.ConfirmarPassword = "diferente";
        if (caso == "telefono") vm.Telefono = "no-numero";
        if (caso == "token") vm.Token = "invalido";
        var service = new ActivacionAdminService(db, null!, null!, TimeProvider.System, null!);
        Assert.False((await service.ActivarAsync(vm)).Exitoso);
        Assert.Equal(0, guard.Attempts); Assert.Empty(db.ChangeTracker.Entries());
    }

    private sealed class Logs : ILogger<ActivacionAdminService>
    {
        public List<string> Messages { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        { Assert.Null(exception); Messages.Add(formatter(state, exception)); }
    }
    private sealed class Correo(bool falla) : EmailService(Config(("Smtp:Host", "smtp.example.invalid"),
        ("Smtp:User", "remitente@example.invalid"), ("Smtp:Password", "unused-fixture"), ("Smtp:Port", "1")))
    {
        public int Attempts;
        protected override Task EnviarMensajeAsync(SmtpClient smtp, MailMessage mail)
        {
            Attempts++;
            Assert.Equal(Email, mail.To.Single().Address);
            Assert.Contains("https://localhost:7175/ActivacionAdmin/Activar#", mail.Body);
            Assert.DoesNotContain("?token", mail.Body);
            return falla ? Task.FromException(new SmtpException("contenido sensible simulado")) : Task.CompletedTask;
        }
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CorreoSimulado_NoSmtpRealNiSecretosEnLogs(bool falla)
    {
        var correo = new Correo(falla); var logs = new Logs(); var token = TokenInvitacion.Generar();
        var service = new ActivacionAdminService(null!, correo, null!, TimeProvider.System, logs);
        var task = (Task<bool>)typeof(ActivacionAdminService).GetMethod("EnviarAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, new object[] { Email, new Uri("https://localhost:7175"), token })!;
        Assert.Equal(!falla, await task); Assert.Equal(1, correo.Attempts);
        Assert.All(logs.Messages, m =>
        {
            var filtraToken = m.Contains(token);
            Assert.False(filtraToken, "El log no debe exponer el token.");
            Assert.DoesNotContain(Email, m); Assert.DoesNotContain("contenido sensible", m);
        });
    }

    [Fact]
    public async Task ControladorConservaAntiforgeryYNoDevuelvePasswordsNiPermiteEmision()
    {
        var post = typeof(ActivacionAdminController).GetMethods().Single(m => m.IsDefined(typeof(HttpPostAttribute)));
        Assert.NotNull(post.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.DoesNotContain(typeof(ActivacionAdminController).GetMethods(), m => m.Name.Contains("Emitir") || m.Name.Contains("Reenviar"));
        var controller = new ActivacionAdminController(null!);
        controller.ModelState.AddModelError("Password", "Inválida");
        controller.ModelState.SetModelValue("Password", "no-retener", "no-retener");
        var vm = new ActivarAdminViewModel { Password = "no-retener", ConfirmarPassword = "no-retener" };
        Assert.IsType<ViewResult>(await controller.Activar(vm));
        Assert.Empty(vm.Password); Assert.Empty(vm.ConfirmarPassword);
        Assert.Null(controller.ModelState["Password"]!.AttemptedValue);
    }

    [Fact]
    public void ContratosEstructurales_BootstrapSinPasswordYTransaccionAntesDeEscrituras()
    {
        // Complementa reglas ejecutables en memoria. Atomicidad efectiva requiere SQL aislado.
        var source = CalificacionEntregaTests.Fuente("Data/InicializadorBD.cs");
        Assert.DoesNotContain("BootstrapAdmin:Password", source);
        Assert.Contains("if (administradorExistente) return null;", source);
        Assert.Contains("ActivacionAdminInicial.CrearMarca", source);
        var service = CalificacionEntregaTests.Fuente("Services/ActivacionAdminService.cs");
        var start = service.IndexOf("public async Task<ServiceResult> ActivarAsync");
        var end = service.IndexOf("internal async Task<ServiceResult> EmitirLocalAsync");
        var method = service[start..end];
        var position = 0;
        foreach (var step in new[] { "PasswordValidator.EsValida", "BeginTransactionAsync", "BloquearAsync", "EsVigente", "BCrypt.Net.BCrypt.HashPassword", "FechaConsumida =", "RegistrarBootstrapAdmin", "SaveChangesAsync", "CommitAsync" })
        { var next = method.IndexOf(step, position); Assert.True(next >= position, step); position = next + step.Length; }
        Assert.DoesNotContain("SignInAsync", CalificacionEntregaTests.Fuente("Controllers/ActivacionAdminController.cs"));
        var program = CalificacionEntregaTests.Fuente("Program.cs");
        Assert.True(program.IndexOf("BootstrapAdminLocal.EjecutarAsync") < program.IndexOf("WebApplication.CreateBuilder"));
        Assert.DoesNotContain("Migrate(", CalificacionEntregaTests.Fuente("Helpers/BootstrapAdminLocal.cs"));
    }
}
