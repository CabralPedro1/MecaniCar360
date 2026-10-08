using System.ComponentModel.DataAnnotations;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace MecaniCar360.Tests;

public class RegistroCompletoClienteTests
{
    [Theory]
    [InlineData("12.345.678", "12345678")]
    [InlineData(" 12-345-678 ", "12345678")]
    [InlineData("0012345678", "12345678")]
    [InlineData("00000000", "0")]
    [InlineData("12345678", "12345678")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("abc123", null)]
    [InlineData("+1234", null)]
    [InlineData("1234567890123456", null)]
    [InlineData("１２３４", null)]
    public void DniCanonico(string? entrada, string? esperado) => Assert.Equal(esperado, DniPersona.Normalizar(entrada));

    [Theory]
    [InlineData("Nombre")]
    [InlineData("Apellido")]
    [InlineData("Dni")]
    [InlineData("Telefono")]
    public void RegistroNoAceptaCamposAusentes(string campo)
    {
        var vm = Completo();
        typeof(RegistroCompletoClienteViewModel).GetProperty(campo)!.SetValue(vm, "");
        Assert.False(RegistroCompletoCliente.DatosValidos(vm));
    }

    [Theory]
    [InlineData("123", false)]
    [InlineData("telefono", false)]
    [InlineData("123456", true)]
    [InlineData("1234567890123456", false)]
    public void TelefonoValidado(string telefono, bool valido)
    {
        var vm = Completo(); vm.Telefono = telefono;
        Assert.Equal(valido, RegistroCompletoCliente.DatosValidos(vm));
    }

    [Fact]
    public void OnboardingPersonalNoAceptaCredenciales()
    {
        Assert.Null(typeof(RegistroCompletoClienteViewModel).GetProperty("Password"));
        Assert.True(RegistroCompletoCliente.DatosValidos(Completo()));
    }

    [Fact]
    public void AplicarDatosNoCambiaIdentidadCorreoRolNiCuenta()
    {
        var usuario = new Usuario { Id = 11, PersonaId = 7 };
        var p = new Persona { Id = 7, Email = "verificado@example.invalid", Usuario = usuario };
        var rol = new PersonaRol { PersonaId = 7, RolId = 3 }; p.Roles.Add(rol);
        var vm = Completo(); vm.Email = "manipulado@example.invalid";
        RegistroCompletoCliente.Aplicar(p, vm);
        Assert.Equal(7, p.Id); Assert.Same(usuario, p.Usuario); Assert.Same(rol, Assert.Single(p.Roles));
        Assert.Equal("verificado@example.invalid", p.Email); Assert.Equal("12345678", p.Dni);
        Assert.True(RegistroCompletoCliente.PersonaCompleta(p));
    }

    [Fact]
    public void MisDatosNoAceptaIdentificadoresNiCredenciales()
    {
        var propiedades = typeof(ContactoClienteViewModel).GetProperties().Select(p => p.Name).ToArray();
        foreach (var n in new[] { "Id", "PersonaId", "UsuarioId", "RolId", "Password", "SecurityStamp" }) Assert.DoesNotContain(n, propiedades);
        foreach (var n in new[] { "Nombre", "Apellido", "Dni", "Email" })
            Assert.NotNull(typeof(ContactoClienteViewModel).GetProperty(n)!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ModelBinding.BindNeverAttribute), true).SingleOrDefault());
    }

    [Fact]
    public void ActivacionExigeDatosAntesDeConsumirToken()
    {
        var vm = new ActivarClienteViewModel { Token = new string('A',43), Username = "cliente", Password = "Prueba123!", ConfirmarPassword = "Prueba123!" };
        Assert.False(RegistroCompletoCliente.DatosValidos(vm));
        vm.Nombre = "Cliente"; vm.Apellido = "Prueba"; vm.Dni = "12345678"; vm.Telefono = "1155555555";
        Assert.True(RegistroCompletoCliente.DatosValidos(vm));
    }

    [Fact]
    public void MigracionProtegeHistoricosAntesDeCrearIndice()
    {
        var m = new MecaniCar360.Migrations.UnicidadDniPersona();
        Assert.Equal(2, m.UpOperations.Count);
        var sql = Assert.IsType<SqlOperation>(m.UpOperations[0]).Sql;
        Assert.Contains("HAVING COUNT(*) > 1", sql); Assert.Contains("TRY_CONVERT", sql);
        Assert.Contains("THROW 51012", sql); Assert.Contains("THROW 51013", sql);
        Assert.DoesNotContain("UPDATE ", sql); Assert.DoesNotContain("DELETE ", sql);
        var i = Assert.IsType<CreateIndexOperation>(m.UpOperations[1]);
        Assert.True(i.IsUnique); Assert.Equal("[Dni] IS NOT NULL", i.Filter); Assert.Equal("Personas", i.Table);
        Assert.IsType<DropIndexOperation>(Assert.Single(m.DownOperations));
    }

    [Fact]
    public void ModeloAlineadoSinConectarALaBase()
    {
        using var db = Contexto();
        Assert.False(db.Database.HasPendingModelChanges());
        var indice = db.Model.FindEntityType(typeof(Persona))!.GetIndexes().Single(i => i.Properties.Single().Name == "Dni");
        Assert.True(indice.IsUnique); Assert.Equal("[Dni] IS NOT NULL", indice.GetFilter());
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Fact]
    public async Task AnonimoSigueSinConsultarLaBase()
    {
        using var db = Contexto(); var invocado = false;
        var middleware = new MecaniCar360.Middleware.PrimerLoginMiddleware(_ => { invocado = true; return Task.CompletedTask; });
        await middleware.InvokeAsync(new Microsoft.AspNetCore.Http.DefaultHttpContext(), db);
        Assert.True(invocado); Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    private static MecaniCarContext Contexto() => new(new DbContextOptionsBuilder<MecaniCarContext>()
        .UseSqlServer("Server=127.0.0.1,1;Database=SoloModelo;Integrated Security=true").Options);
    private static RegistroCompletoClienteViewModel Completo() => new() { Nombre = "Cliente", Apellido = "Prueba",
        Dni = "12.345.678", Telefono = "1155555555" };
}
