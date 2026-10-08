using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using MecaniCar360.Controllers;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Tests;

public class StockPresupuestoRegresionTests
{
    private static string Fuente(string ruta)
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta != null && !File.Exists(Path.Combine(carpeta.FullName, "MecaniCar360.csproj"))) carpeta = carpeta.Parent;
        Assert.NotNull(carpeta);
        return File.ReadAllText(Path.Combine(carpeta!.FullName, ruta));
    }

    [Fact]
    public void Presupuesto_UsaCatalogoTecnicoDespuesDeAutorizarOrden_YMuestraFallo()
    {
        var fuente = Fuente("Controllers/PresupuestoController.cs");
        var detalle = fuente[fuente.IndexOf("public async Task<IActionResult> Detalle")..fuente.IndexOf("public async Task<IActionResult> Obtener")];
        Assert.Contains("_stock.ObtenerRepuestosTecnicosAsync(usuario)", detalle);
        Assert.DoesNotContain("_stock.ObtenerRepuestosAsync", detalle);
        Assert.True(detalle.IndexOf("if (!resultado.Exitoso)") < detalle.IndexOf("_stock.ObtenerRepuestosTecnicosAsync"));
        Assert.Contains("if (!repuestos.Exitoso)", detalle);
        Assert.Contains("ViewData[\"ErrorRepuestos\"]", Fuente("Views/Presupuesto/Detalle.cshtml"));
        var tecnico = Fuente("Services/StockService.ConsultasTecnicas.cs");
        Assert.Contains("\"STOCK_VER\"", tecnico);
        Assert.DoesNotContain("PROVEEDOR_VER", tecnico);
        Assert.DoesNotContain("Include(", tecnico);
        var propiedades = typeof(RepuestoTecnicoDto).GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain(propiedades, p => p.Contains("Proveedor") || p.Contains("Precio") || p.Contains("Costo"));
    }

    [Theory]
    [InlineData("Crear")]
    [InlineData("Editar")]
    public void Formularios_PresentanErroresYConservanBinding(string accion)
    {
        var vista = Fuente($"Views/Stock/{accion}.cshtml");
        Assert.Contains("method=\"post\"", vista);
        Assert.Contains("asp-validation-summary=\"All\"", vista);
        foreach (var campo in new[] { "SKU", "Nombre", "Marca", "Modelo", "Compatibilidad", "PrecioVenta", "StockMinimo" })
        {
            Assert.Contains($"asp-for=\"{campo}\"", vista);
            Assert.Contains($"asp-validation-for=\"{campo}\"", vista);
        }
        Assert.DoesNotContain("asp-for=\"StockActual\"", vista);
        if (accion == "Crear") Assert.Contains("stock cero", vista);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Stock_RechazoConservaModeloYExponeError(bool editar, bool modelStateInvalido)
    {
        using var conexion = new ConexionLectura(new DataTable());
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(conexion).Options);
        var permisos = new PermisoService(db);
        var stock = new StockService(db, permisos, null!, null!);
        var controller = new StockController(stock, new ProveedorService(db, permisos, null!), db, permisos)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var modelo = new Repuesto { Id = 7, SKU = "  ORIGINAL  ", Nombre = "Ingresado", PrecioVenta = 12.50m, StockMinimo = 3 };
        if (modelStateInvalido) controller.ModelState.AddModelError("Nombre", "Nombre inválido");
        // Actor inexistente: rechazo real del servicio, sin tocar SQL ni simular ?xito.
        var resultado = Assert.IsType<ViewResult>(editar ? await controller.Editar(modelo) : await controller.Crear(modelo));
        Assert.Same(modelo, resultado.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState.Values.SelectMany(v => v.Errors), e =>
            e.ErrorMessage == (modelStateInvalido ? "Nombre inválido" : "Acceso denegado."));
        Assert.Equal("  ORIGINAL  ", modelo.SKU);
        Assert.Equal(12.50m, modelo.PrecioVenta);
    }

    [Theory]
    [InlineData("", false, "Debe ingresar un nombre.")]
    [InlineData("   ", false, "Debe ingresar un nombre.")]
    [InlineData("Válido", true, "Ya existe un repuesto con ese SKU.")]
    [InlineData("Válido", false, "")]
    public async Task ValidacionExistente_RechazaNombreYSkuDuplicado(string nombre, bool duplicado, string error)
    {
        var tabla = new DataTable(); tabla.Columns.Add("Existe", typeof(bool)); tabla.Rows.Add(duplicado);
        using var conexion = new ConexionLectura(tabla);
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(conexion).Options);
        var servicio = new StockService(db, null!, null!, null!);
        var metodo = typeof(StockService).GetMethod("ValidarRepuestoAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var resultado = await (Task<ServiceResult>)metodo.Invoke(servicio,
            new object?[] { new Repuesto { SKU = "SKU", Nombre = nombre, PrecioVenta = 10m }, null })!;
        Assert.Equal(error.Length == 0, resultado.Exitoso);
        Assert.Equal(error, resultado.Mensaje);
    }
    private sealed class ConexionLectura(params DataTable[] resultados) : DbConnection
    {
        private ConnectionState estado;
        public int Lecturas { get; private set; }
        private DbDataReader Leer() => resultados[Lecturas++].CreateDataReader();
        [AllowNull] public override string ConnectionString { get; set; } = "";
        public override string Database => "PruebaSinBase";
        public override string DataSource => "Prueba";
        public override string ServerVersion => "16.0";
        public override ConnectionState State => estado;
        public override void Open() => estado = ConnectionState.Open;
        public override void Close() => estado = ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new Comando(this);
        private sealed class Comando(ConexionLectura conexion) : DbCommand
        {
            private readonly Microsoft.Data.SqlClient.SqlCommand parametros = new();
            [AllowNull] public override string CommandText { get; set; } = "";
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get; set; } = conexion;
            protected override DbTransaction? DbTransaction { get; set; }
            protected override DbParameterCollection DbParameterCollection => parametros.Parameters;
            protected override DbParameter CreateDbParameter() => parametros.CreateParameter();
            public override void Cancel() { }
            public override void Prepare() { }
            public override int ExecuteNonQuery() => throw new NotSupportedException("No se permiten escrituras.");
            public override object ExecuteScalar() => throw new NotSupportedException();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
                conexion.Leer();
        }
    }
}
