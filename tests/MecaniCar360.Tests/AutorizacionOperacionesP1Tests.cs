using System.Reflection;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Tests;

// Ejecuta los filtros reales en memoria y su traducción SQL sin abrir conexiones.
// Las comprobaciones de integración por fuente no sustituyen una operación SQL.
public class AutorizacionOperacionesP1Tests
{
    private static IQueryable<Usuario> Filtrar(bool asignacion, IQueryable<Usuario> usuarios, int clienteId = 9)
    {
        var tipo = asignacion ? typeof(OrdenTrabajoService) : typeof(PresupuestoService);
        var metodo = tipo.GetMethod(asignacion ? "AdministradoresParaAsignacion" : "ClientesParaDecision",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(metodo);
        return Assert.IsAssignableFrom<IQueryable<Usuario>>(metodo.Invoke(null,
            asignacion ? new object[] { usuarios } : new object[] { usuarios, clienteId }));
    }

    [Theory]
    [InlineData(9, true)]
    [InlineData(10, false)]
    public void Decision_ExigeClientePropietario(int propietario, bool permitido)
    {
        Assert.Equal(permitido, Filtrar(false, new[] { Cuenta("CLIENTE") }.AsQueryable(), propietario).Any());
    }

    [Theory]
    [InlineData(true, "ADMIN", true)]
    [InlineData(true, "CAJA", false)]
    [InlineData(true, "MECANICO", false)]
    [InlineData(true, "STOCK", false)]
    [InlineData(true, "CLIENTE", false)]
    [InlineData(false, "CLIENTE", true)]
    [InlineData(false, "ADMIN", false)]
    [InlineData(false, "CAJA", false)]
    [InlineData(false, "MECANICO", false)]
    [InlineData(false, "STOCK", false)]
    public void RolEfectivo_EsNecesarioAunquePoseaLaPatente(bool asignacion, string rol, bool permitido)
    {
        var usuario = Cuenta(rol);
        Assert.Equal(permitido, Filtrar(asignacion, new[] { usuario }.AsQueryable()).Any());
        Assert.True(usuario.Activo);
        Assert.Single(usuario.Persona.Roles);
    }

    [Theory]
    [InlineData(true, "usuario")]
    [InlineData(true, "persona")]
    [InlineData(true, "rol")]
    [InlineData(true, "baja")]
    [InlineData(false, "usuario")]
    [InlineData(false, "persona")]
    [InlineData(false, "rol")]
    [InlineData(false, "baja")]
    public void CuentaPersonaORolInhabilitado_NoAutoriza(bool asignacion, string desactivar)
    {
        var u = Cuenta(asignacion ? "ADMIN" : "CLIENTE");
        if (desactivar == "usuario") u.Activo = false;
        if (desactivar == "persona") u.Persona.Activo = false;
        if (desactivar == "rol") u.Persona.Roles[0].Rol.Activo = false;
        if (desactivar == "baja") u.Persona.Roles[0].FechaBaja = DateTime.Today;
        Assert.Empty(Filtrar(asignacion, new[] { u }.AsQueryable()));
    }

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("CAJA")]
    [InlineData("MECANICO")]
    [InlineData("STOCK")]
    public void PerfilInternoConRolCliente_NoPuedeDecidir(string interno)
    {
        var u = Cuenta("CLIENTE");
        u.Persona.Roles.AddRange(Cuenta(interno).Persona.Roles);
        Assert.Empty(Filtrar(false, new[] { u }.AsQueryable()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Filtros_SeTraducenASqlSinConexionNiTracking(bool asignacion)
    {
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=SoloMetadatos;Integrated Security=true;Connect Timeout=1").Options);
        var sql = Filtrar(asignacion, db.Usuarios.AsNoTracking()).Where(u => u.Id == 7).ToQueryString();
        Assert.Contains("EXISTS", sql);
        Assert.Contains("FechaBaja", sql);
        Assert.Contains(asignacion ? "ADMIN" : "CLIENTE", sql);
        if (!asignacion) Assert.Contains("NOT EXISTS", sql);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public void Decisiones_RevalidanClienteYOwnershipAntesDeEfectos_ConsultaAdminSeConserva()
    {
        var s = Fuente("Services/PresupuestoService.cs");
        var decision = s[s.IndexOf("private async Task<ServiceResult<PresupuestoOperacion>> DecidirAsync", StringComparison.Ordinal)..];
        decision = decision[..decision.IndexOf("private async Task<ServiceResult<PresupuestoOperacion>> EjecutarAsync", StringComparison.Ordinal)];
        Assert.Contains("ClienteDecisorAsync(solicitante, versionId)", decision);
        Assert.True(decision.IndexOf("ClienteDecisorAsync(usuario, versionId)", StringComparison.Ordinal) <
            decision.IndexOf("ProcesarAprobacionIncrementalAsync", StringComparison.Ordinal));
        Assert.DoesNotContain("PropietarioAsync(", decision);
        Assert.Contains("EstaHabilitadoAsync(usuario.PersonaId)", s);
        Assert.Contains("v.Presupuesto.OrdenTrabajo.IngresoVehiculo.Turno.ClienteId", s);
        Assert.Contains("ClientesParaDecision(_context.Usuarios.AsNoTracking(), clienteId.Value)", s);
        Assert.Contains("private async Task<bool> PropietarioAsync(OrdenTrabajo orden, Usuario usuario) =>\n            await _permisos.EsAdministradorAsync(usuario.Id) ||", s);
        var asignacion = Fuente("Services/OrdenTrabajoService.cs");
        asignacion = asignacion[asignacion.IndexOf("            AsignarMecanicoAsync(", StringComparison.Ordinal)..];
        asignacion = asignacion[..asignacion.IndexOf("            TomarOrdenAsync(", StringComparison.Ordinal)];
        Assert.Equal(2, asignacion.Split("AdministradoresParaAsignacion(_context.Usuarios.AsNoTracking())").Length - 1);
        Assert.True(asignacion.IndexOf("AdministradoresParaAsignacion", StringComparison.Ordinal) <
            asignacion.IndexOf("BeginTransactionAsync", StringComparison.Ordinal));
        Assert.Contains("ORDEN_ASIGNAR_MECANICO", asignacion);
    }

    private static Usuario Cuenta(string rol) => new()
    {
        Id = 7, Username = "fixture", EmailLogin = "fixture@example.invalid", PersonaId = 9,
        Persona = new Persona { Id = 9, Roles = new() { new() { Rol = new Rol { Nombre = rol, Activo = true } } } }
    };

    private static string Fuente(string archivo)
    {
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz != null && !File.Exists(Path.Combine(raiz.FullName, "MecaniCar360.csproj"))) raiz = raiz.Parent;
        Assert.NotNull(raiz);
        return File.ReadAllText(Path.Combine(raiz.FullName, archivo)).Replace("\r\n", "\n");
    }
}
