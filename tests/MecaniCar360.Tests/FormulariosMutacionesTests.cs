using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using MecaniCar360.Controllers;
using MecaniCar360.Data;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Tests;

public class FormulariosMutacionesTests
{
    private static void Identidad(Controller controller) => controller.ControllerContext = new()
    {
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "7") }, "Test")) }
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Garantia_RechazoConservaErroresYSeleccion_SinEscrituras(bool invalido)
    {
        using var connection = new SoloLecturaVacia();
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(connection).Options);
        var controller = new GarantiaController(new GarantiaService(db, new PermisoService(db), null!, null!, null!));
        Identidad(controller);
        controller.ModelState.SetModelValue("items[12].MesesGarantia", "abc", "abc");
        if (invalido) controller.ModelState.AddModelError("items[12].MesesGarantia", "Meses inválidos");
        var result = Assert.IsType<ViewResult>(await controller.Crear(4, new List<GarantiaItemDto> { new() { FacturaItemId = 12 } }));
        Assert.Equal(4, Assert.IsType<CrearGarantiaViewModel>(result.Model).OrdenTrabajoId);
        Assert.Contains(12, Assert.IsType<HashSet<int>>(controller.ViewData["ItemsSeleccionados"]));
        Assert.Equal("abc", controller.ModelState["items[12].MesesGarantia"]!.AttemptedValue);
        Assert.Contains(controller.ModelState.Values.SelectMany(v => v.Errors), e => e.ErrorMessage == "Acceso denegado.");
        Assert.True(connection.Reads > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Calificacion_RechazoNoExponeOrdenAjenaNiEscribe(bool errorDeBinding)
    {
        using var connection = new SoloLecturaVacia();
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(connection).Options);
        var permisos = new PermisoService(db);
        var controller = new CalificacionController(new CalificacionTrabajoService(db, permisos),
            new OrdenTrabajoService(db, null!, permisos, null!, null!, null!), permisos);
        Identidad(controller);
        if (errorDeBinding) controller.ModelState.AddModelError("Puntuacion", "Puntuación inválida");
        // Out-of-range input is rejected by the real service before any transaction.
        Assert.IsType<NotFoundResult>(await controller.Crear(new CalificacionViewModel
            { OrdenTrabajoId = 4, Puntuacion = 0, Comentario = "Conservar sólo para el propietario" }));
        Assert.False(controller.ModelState.IsValid);
        Assert.True(connection.Reads > 0);
    }

    [Theory]
    [InlineData("ingreso")]
    [InlineData("salida")]
    [InlineData("ajuste")]
    public async Task Stock_FormularioInvalidoNoOmitePermisoDeConsulta(string accion)
    {
        using var connection = new SoloLecturaVacia();
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(connection).Options);
        var controller = new StockController(null!, null!, db, new PermisoService(db));
        Identidad(controller);
        controller.ModelState.AddModelError("cantidad", "Cantidad inválida");
        var result = accion switch
        {
            "ingreso" => await controller.RegistrarIngreso(3, 4, 0, 5, "Texto"),
            "salida" => await controller.RegistrarSalida(3, 0, 4, "Texto"),
            _ => await controller.RegistrarAjuste(3, 0, "Texto")
        };
        Assert.IsType<ForbidResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public void VistasConservanContratosYRenderizanValoresRechazados()
    {
        // Structural checks complement the executable rejection tests; not a browser test.
        var stock = CalificacionEntregaTests.Fuente("Views/Stock/Detalle.cshtml");
        foreach (var accion in new[] { "RegistrarIngreso", "RegistrarSalida", "RegistrarAjuste" })
        {
            Assert.Contains($"asp-action=\"{accion}\" method=\"post\"", stock);
            Assert.Contains($"@Valor(\"{accion}\",", stock);
        }
        var garantia = CalificacionEntregaTests.Fuente("Views/Garantia/Crear.cshtml");
        Assert.Contains("@Html.TextBox($\"items[{item.Id}].MesesGarantia\"", garantia);
        Assert.Contains("@Html.ValidationMessage($\"items[{item.Id}].MesesGarantia\"", garantia);
        Assert.Contains("@Html.AntiForgeryToken()", garantia);
        var calificacion = CalificacionEntregaTests.Fuente("Controllers/CalificacionController.cs");
        Assert.Contains("return View(\"Detalle\", model)", calificacion);
        Assert.Contains("if (!ModelState.IsValid) return await MostrarDetalle(model);", calificacion);
        Assert.Contains("ModelState.AddModelError(string.Empty, r.Mensaje)", calificacion);
        Assert.Contains("RedirectToAction(nameof(Detalle)", calificacion);
    }

    [Theory]
    [InlineData("Finalizar")]
    [InlineData("ActualizarObservaciones")]
    [InlineData("ActualizarCostoDiagnostico")]
    public async Task Orden_ErrorDeBindingNoInvocaEscrituraNiOmitePermisoDeDetalle(string accion)
    {
        using var connection = new SoloLecturaVacia();
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(connection).Options);
        var controller = new OrdenTrabajoController(null!, new PermisoService(db));
        Identidad(controller);
        controller.ModelState.AddModelError("campo", "Formato inválido");
        var result = accion switch
        {
            "Finalizar" => await controller.Finalizar(3, null),
            "ActualizarObservaciones" => await controller.ActualizarObservaciones(3, "Texto"),
            _ => await controller.ActualizarCostoDiagnostico(3, 0)
        };
        Assert.IsType<ForbidResult>(result);
        Assert.True(connection.Reads > 0);
    }

    [Fact]
    public async Task Reprogramacion_ErrorDeBindingRevalidaConsultaSinMutar()
    {
        using var connection = new SoloLecturaVacia();
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(connection).Options);
        var service = new TurnoService(db, null!, new PermisoService(db), null!, null!, null!);
        var controller = new TurnoController(service, null!, null!, null!, null!);
        Identidad(controller);
        controller.ModelState.SetModelValue("nuevaFechaInicio", "incorrecta", "incorrecta");
        controller.ModelState.AddModelError("nuevaFechaInicio", "Formato inválido");
        Assert.IsType<NotFoundResult>(await controller.Reprogramar(3, default));
        Assert.Equal("incorrecta", controller.ModelState["nuevaFechaInicio"]!.AttemptedValue);
        Assert.True(connection.Reads > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Diagnostico_RechazoRevalidaLecturaYNoExponeOrden(bool bindingInvalido)
    {
        using var connection = new SoloLecturaVacia();
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(connection).Options);
        var permisos = new PermisoService(db);
        var controller = new DiagnosticoController(new DiagnosticoService(db, null!, permisos, null!),
            new OrdenTrabajoService(db, null!, permisos, null!, null!, null!));
        Identidad(controller);
        controller.ModelState.SetModelValue("descripcion", "Texto ingresado", "Texto ingresado");
        if (bindingInvalido) controller.ModelState.AddModelError("descripcion", "Formato inválido");
        Assert.IsType<NotFoundResult>(await controller.Guardar(3, "Texto ingresado"));
        Assert.Equal("Texto ingresado", controller.ModelState["descripcion"]!.AttemptedValue);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(connection.Reads > 0);
    }

    [Theory]
    [InlineData("AgregarItem")]
    [InlineData("EliminarItem")]
    [InlineData("EnviarAprobacion")]
    [InlineData("Aprobar")]
    [InlineData("Rechazar")]
    public async Task Presupuesto_ContextoDePantallaNoAutorizaLecturaNiMutacion(string accion)
    {
        using var connection = new SoloLecturaVacia();
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(connection).Options);
        var service = new PresupuestoService(db, new PermisoService(db), null!, null!, null!, null!, null!, null!);
        var controller = new PresupuestoController(service, null!);
        Identidad(controller);
        controller.ModelState.AddModelError("campo", "Formato inválido");
        var result = accion switch
        {
            "AgregarItem" => await controller.AgregarItem(2, "Texto", 1, 2m, 4, 999),
            "EliminarItem" => await controller.EliminarItem(2, 4, 999),
            "EnviarAprobacion" => await controller.EnviarAprobacion(2, new[] { 8 }, 999),
            "Aprobar" => await controller.Aprobar(2, 999),
            _ => await controller.Rechazar(2, "Motivo", 999)
        };
        Assert.Equal("ErrorOperacion", Assert.IsType<ViewResult>(result).ViewName);
        Assert.True(connection.Reads > 0);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void Presupuesto_RechazoConservaEntradaPeroNoSustituyeRecursoOVersion(bool cliente, bool mismoRecurso)
    {
        var controller = new PresupuestoController(null!, null!);
        Identidad(controller);
        controller.ModelState.SetModelValue("motivo", "Mi motivo", "Mi motivo");
        controller.ModelState.AddModelError(string.Empty, "Rechazo de negocio");
        object model = cliente ? new MecaniCar360.Models.PresupuestoVersion { Id = 7 }
            : new MecaniCar360.Models.Presupuesto { Id = 7 };
        var vista = controller.View(model);
        var method = typeof(PresupuestoController).GetMethod("PresentarRechazo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var result = Assert.IsType<ViewResult>(method.Invoke(controller, new object[] { vista, mismoRecurso ? 7 : 8, cliente }));
        if (mismoRecurso)
        {
            Assert.Same(model, result.Model);
            Assert.Equal(cliente ? "Propio" : "Detalle", result.ViewName);
            Assert.Equal("Mi motivo", result.ViewData.ModelState["motivo"]!.AttemptedValue);
            Assert.Contains(result.ViewData.ModelState[string.Empty]!.Errors, e => e.ErrorMessage == "Rechazo de negocio");
        }
        else
        {
            Assert.Equal("ErrorOperacion", result.ViewName);
            Assert.IsType<string>(result.Model); // no replacement version/form is offered
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AsociarProveedor_EntradaRechazadaNoOmitePermisos(bool bindingInvalido)
    {
        using var connection = new SoloLecturaVacia();
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(connection).Options);
        var permisos = new PermisoService(db);
        var controller = new StockController(new StockService(db, permisos, null!, null!), null!, db, permisos);
        Identidad(controller);
        controller.ModelState.SetModelValue("CodigoProveedor", "Código ingresado", "Código ingresado");
        if (bindingInvalido) controller.ModelState.AddModelError("PrecioCompra", "Precio inválido");
        Assert.IsType<ForbidResult>(await controller.AgregarProveedor(new AdministrarProveedoresViewModel
            { RepuestoId = 4, ProveedorId = 8, CodigoProveedor = "Código ingresado", PrecioCompra = 1 }));
        Assert.Equal("Código ingresado", controller.ModelState["CodigoProveedor"]!.AttemptedValue);
    }

    [Fact]
    public void Presupuesto_RepuestoRetiradoNoSeConvierteSilenciosamenteEnServicio()
    {
        var controller = new PresupuestoController(null!, null!);
        Identidad(controller);
        controller.ViewData["OperacionRechazada"] = "AgregarItem";
        controller.ViewData["Repuestos"] = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
        controller.ModelState.SetModelValue("repuestoId", "9", "9");
        var vista = controller.View(new MecaniCar360.Models.Presupuesto { Id = 7 });
        var method = typeof(PresupuestoController).GetMethod("PresentarRechazo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var result = Assert.IsType<ViewResult>(method.Invoke(controller, new object[] { vista, 7, false }));
        Assert.Equal("Detalle", result.ViewName);
        Assert.Single(result.ViewData.ModelState["repuestoId"]!.Errors);
        Assert.Equal("9", result.ViewData.ModelState["repuestoId"]!.AttemptedValue);
    }

    [Fact]
    public void Asociaciones_DatosDePresentacionNoSeRecibenNiValidanComoEntrada()
    {
        foreach (var nombre in new[] { "Repuesto", "Proveedores", "ProveedoresDisponibles" })
        {
            var property = typeof(AdministrarProveedoresViewModel).GetProperty(nombre)!;
            Assert.True(Attribute.IsDefined(property, typeof(Microsoft.AspNetCore.Mvc.ModelBinding.BindNeverAttribute)));
            Assert.True(Attribute.IsDefined(property, typeof(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNeverAttribute)));
        }
    }

    [Fact]
    public void FormulariosRecuperables_ConservanCamposYContextosSinDuplicarDiagnostico()
    {
        var diagnostico = CalificacionEntregaTests.Fuente("Views/Diagnostico/_Formulario.cshtml");
        Assert.Contains("asp-controller=\"Diagnostico\"", diagnostico);
        Assert.Contains("@ViewData.ModelState[\"descripcion\"]?.AttemptedValue", diagnostico);
        Assert.Contains("@Html.ValidationMessage(\"descripcion\"", diagnostico);
        Assert.Contains("@Html.AntiForgeryToken()", diagnostico);
        Assert.DoesNotContain("name=\"descripcion\"", CalificacionEntregaTests.Fuente("Views/OrdenTrabajo/Detalle.cshtml"));
        var presupuesto = CalificacionEntregaTests.Fuente("Views/Presupuesto/Detalle.cshtml");
        foreach (var campo in new[] { "descripcion", "cantidad", "precioUnitario", "repuestoId" })
            Assert.Contains($"@Html.ValidationMessage(\"{campo}\"", presupuesto);
        Assert.Contains("EvidenciasSeleccionadas", presupuesto);
        var proveedores = CalificacionEntregaTests.Fuente("Views/Stock/AdministrarProveedores.cshtml");
        Assert.Contains("relacionPrecioRechazada == item.Id", proveedores);
        Assert.Contains("relacionPrecioRechazada.HasValue", proveedores);
    }

    // In-memory ADO.NET double: every read finds no user. No network and every write/transaction fails.
    private sealed class SoloLecturaVacia : DbConnection
    {
        private ConnectionState state;
        public int Reads { get; private set; }
        [AllowNull] public override string ConnectionString { get; set; } = "";
        public override string Database => "SinBase";
        public override string DataSource => "EnMemoria";
        public override string ServerVersion => "16.0";
        public override ConnectionState State => state;
        public override void Open() => state = ConnectionState.Open;
        public override void Close() => state = ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException("No se permiten escrituras.");
        protected override DbCommand CreateDbCommand() => new Command(this);
        private sealed class Command(SoloLecturaVacia connection) : DbCommand
        {
            private readonly Microsoft.Data.SqlClient.SqlCommand parameters = new();
            [AllowNull] public override string CommandText { get; set; } = "";
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get; set; } = connection;
            protected override DbTransaction? DbTransaction { get; set; }
            protected override DbParameterCollection DbParameterCollection => parameters.Parameters;
            protected override DbParameter CreateDbParameter() => parameters.CreateParameter();
            public override void Cancel() { }
            public override void Prepare() { }
            public override int ExecuteNonQuery() => throw new NotSupportedException("No se permiten escrituras.");
            public override object ExecuteScalar() => throw new NotSupportedException();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            { connection.Reads++; return new DataTable().CreateDataReader(); }
        }
    }
}
