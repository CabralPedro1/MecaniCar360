using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MecaniCar360.Tests;

public class OnboardingClienteTests
{
    [Fact]
    public void MigracionPublica_SoloCambiaNulabilidad_YDownNoInventaEmisor()
    {
        var migration = new MecaniCar360.Migrations.PermitirInvitacionClientePublica();
        var up = Assert.IsType<Microsoft.EntityFrameworkCore.Migrations.Operations.AlterColumnOperation>(Assert.Single(migration.UpOperations));
        Assert.Equal("InvitacionesCliente", up.Table);
        Assert.Equal("EmitidaPorUsuarioId", up.Name);
        Assert.True(up.IsNullable);
        Assert.Equal(2, migration.DownOperations.Count);
        var guard = Assert.IsType<Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation>(migration.DownOperations[0]);
        Assert.Contains("IS NULL", guard.Sql);
        Assert.Contains("THROW", guard.Sql);
        var down = Assert.IsType<Microsoft.EntityFrameworkCore.Migrations.Operations.AlterColumnOperation>(migration.DownOperations[1]);
        Assert.False(down.IsNullable);
        Assert.Null(down.DefaultValue);
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("id-ficticio", "", false)]
    [InlineData("", "secreto-ficticio", false)]
    [InlineData("id-ficticio", "secreto-ficticio", true)]
    public void Google_SoloDisponibleConAmbosValores(string? id, string? secret, bool esperado)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Authentication:Google:ClientId"] = id, ["Authentication:Google:ClientSecret"] = secret }).Build();
        Assert.Equal(esperado, GoogleClienteConfiguracion.Disponible(config));
    }

    [Fact]
    public void InvitacionPublica_FkOpcional_MantieneRestrict()
    {
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=SoloModelo;Integrated Security=true").Options);
        var entidad = db.Model.FindEntityType(typeof(InvitacionCliente))!;
        Assert.True(entidad.FindProperty(nameof(InvitacionCliente.EmitidaPorUsuarioId))!.IsNullable);
        Assert.Equal(DeleteBehavior.Restrict, entidad.GetForeignKeys().Single(f =>
            f.Properties.Any(p => p.Name == nameof(InvitacionCliente.EmitidaPorUsuarioId))).DeleteBehavior);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Theory]
    [InlineData(null, null, "Solicitud web")]
    [InlineData(1, "operador", "Emitida por operador")]
    public void PresentacionEmisor(int? id, string? nombre, string esperado) =>
        Assert.Equal(esperado, OrigenInvitacion.Describir(id, nombre));

    [Fact]
    public void PersonaParcial_NoInventaDatos_YPermiteDetectarDniPendiente()
    {
        var persona = new Persona { Nombre = "Cliente", Apellido = "Prueba", Email = "cliente@example.invalid" };
        Assert.Equal(new[] { "DNI" }, DatosRecepcionCliente.Faltantes(persona));
        persona.Dni = "12345678";
        Assert.Empty(DatosRecepcionCliente.Faltantes(persona));
        Assert.Null(persona.Telefono);
        Assert.Null(persona.Usuario);
    }

    [Theory]
    [InlineData(null, null, null, 3)]
    [InlineData("  ", "Prueba", "123", 1)]
    [InlineData("Cliente", "", "123", 1)]
    public void DatosRecepcion_CentralizaFaltantes(string? nombre, string? apellido, string? dni, int total) =>
        Assert.Equal(total, DatosRecepcionCliente.Faltantes(new Persona { Nombre = nombre, Apellido = apellido, Dni = dni }).Count);

    [Theory]
    [InlineData("", "Apellido", "cliente@example.invalid", false)]
    [InlineData("Nombre", "", "cliente@example.invalid", false)]
    [InlineData("Nombre", "Apellido", "invalido", false)]
    [InlineData("Nombre", "Apellido", "", false)]
    [InlineData("Nombre", "Apellido", "cliente@example.invalid", true)]
    public void Solicitud_ValidaDatosMinimos(string nombre, string apellido, string email, bool valido)
    {
        var vm = new RegistroClienteViewModel { Nombre = nombre, Apellido = apellido, Email = email };
        Assert.Equal(valido, Validator.TryValidateObject(vm, new(vm), new List<ValidationResult>(), true));
    }

    [Theory]
    [InlineData("subject", "cliente@example.invalid", "true", true, true)]
    [InlineData("subject", "cliente@example.invalid", "false", true, false)]
    [InlineData("subject", "cliente@example.invalid", "", true, false)]
    [InlineData("", "cliente@example.invalid", "true", true, false)]
    [InlineData("subject", "invalido", "true", true, false)]
    [InlineData("subject", "cliente@example.invalid", "true", false, false)]
    public void Google_ExigeIdentidadYEmailVerificados(string subject, string email, string verificado, bool autenticado, bool valido)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim(ClaimTypes.Email, email), new Claim(GoogleClienteConfiguracion.EmailVerificado, verificado) }, autenticado ? "Google" : null));
        var resultado = IdentidadGoogleVerificada.Leer(principal);
        Assert.Equal(valido, resultado != null);
        if (valido) Assert.Equal(subject, resultado!.Subject);
    }

    [Fact]
    public async Task Google_SinConfiguracion_NoRegistraProvider()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddAuthentication();
        services.AgregarGoogleCliente(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        Assert.Null(await provider.GetRequiredService<IAuthenticationSchemeProvider>().GetSchemeAsync("Google"));
    }

    [Fact]
    public void Google_ConfiguraSubjectVerificado_SinGuardarTokens()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Authentication:Google:ClientId"] = "ficticio", ["Authentication:Google:ClientSecret"] = "ficticio" }).Build();
        var services = new ServiceCollection().AddLogging();
        services.AgregarGoogleCliente(config);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<GoogleOptions>>().Get("Google");
        Assert.False(options.SaveTokens);
        Assert.Equal(GoogleClienteConfiguracion.Externa, options.SignInScheme);
        Assert.Equal("/signin-google", options.CallbackPath.Value);
        var claims = new ClaimsIdentity("Google");
        using var json = System.Text.Json.JsonDocument.Parse("{\"sub\":\"stable-sub\",\"email\":\"cliente@example.invalid\",\"email_verified\":true}");
        foreach (var action in options.ClaimActions) action.Run(json.RootElement, claims, "Google");
        Assert.Equal("stable-sub", IdentidadGoogleVerificada.Leer(new ClaimsPrincipal(claims))!.Subject);
    }
}
