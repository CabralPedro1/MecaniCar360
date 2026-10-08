using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using MecaniCar360.Controllers;
using MecaniCar360.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MecaniCar360.Tests;

public class NavegacionCierreTests
{
    [Theory]
    [InlineData("Views/Account/AccesoDenegado.cshtml")]
    [InlineData("Views/Factura/RegistrarPago.cshtml")]
    [InlineData("Views/Auditoria/Index.cshtml")]
    public void RetornoGeneral_NoExigePermisoDashboard(string archivo)
    {
        var vista = Fuente(archivo);
        Assert.Contains("asp-controller=\"Account\" asp-action=\"Inicio\"", vista);
        Assert.DoesNotContain("asp-controller=\"Dashboard\"", vista);
        if (archivo.Contains("RegistrarPago"))
        {
            Assert.Contains("asp-action=\"PorId\"", vista);
            Assert.Contains("FACTURA_VER", vista);
        }
    }

    private static string Fuente(string ruta)
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio != null && !File.Exists(Path.Combine(directorio.FullName, "MecaniCar360.csproj")))
            directorio = directorio.Parent;
        Assert.NotNull(directorio);
        return File.ReadAllText(Path.Combine(directorio!.FullName, ruta));
    }

    private static PresupuestoController Presupuesto(bool autenticado = true)
    {
        var http = new DefaultHttpContext();
        if (autenticado) http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "7") }, "Prueba"));
        return new PresupuestoController(null!, null!)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Theory]
    [InlineData("Crear", false)]
    [InlineData("AgregarItem", false)]
    [InlineData("EliminarItem", false)]
    [InlineData("EnviarAprobacion", false)]
    [InlineData("Aprobar", true)]
    [InlineData("Rechazar", true)]
    public async Task Presupuesto_ModelStateInvalido_RenderizaErrorSinInvocarNegocio(string accion, bool cliente)
    {
        var controller = Presupuesto();
        controller.ModelState.AddModelError("id", "Valor inválido");
        var resultado = accion switch
        {
            "Crear" => await controller.Crear(1),
            "AgregarItem" => await controller.AgregarItem(1, "Texto", 1, 1m, null),
            "EliminarItem" => await controller.EliminarItem(1, 2),
            "EnviarAprobacion" => await controller.EnviarAprobacion(1),
            "Aprobar" => await controller.Aprobar(1),
            _ => await controller.Rechazar(1, "Motivo")
        };
        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Equal("ErrorOperacion", vista.ViewName);
        Assert.IsType<string>(vista.Model);
        Assert.Equal(cliente, vista.ViewData["EsCliente"]);
        Assert.Equal(400, controller.Response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Presupuesto_RechazoServicioConservaMensajeYContexto(bool cliente)
    {
        var controller = Presupuesto();
        var metodo = typeof(PresupuestoController).GetMethod("Resultado", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var resultado = metodo.Invoke(controller, new object[]
        {
            ServiceResult<PresupuestoOperacion>.Error("La versión ya no admite decisiones."), cliente
        });
        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Equal("ErrorOperacion", vista.ViewName);
        Assert.Equal("La versión ya no admite decisiones.", vista.Model);
        Assert.Equal(cliente, vista.ViewData["EsCliente"]);
    }

    [Fact]
    public async Task Presupuesto_SinIdentidadConservaRechazo()
    {
        Assert.IsType<ForbidResult>(await Presupuesto(false).Crear(1));
    }

    [Fact]
    public async Task Inicio_AnonimoVaALogin()
    {
        var controller = new AccountController(null!, null!, null!, null!, null!)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var resultado = Assert.IsType<RedirectToActionResult>(await controller.Inicio());
        Assert.Equal("Login", resultado.ActionName);
    }

    [Fact]
    public async Task Inicio_IdentidadMalformadaNoResuelveDestino()
    {
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "inválido") }, "Prueba")) };
        var controller = new AccountController(null!, null!, null!, null!, null!)
        { ControllerContext = new ControllerContext { HttpContext = http } };
        Assert.IsType<ForbidResult>(await controller.Inicio());
    }

    [Fact]
    public void ConsultaTecnica_ContratoMvcSinInformacionComercial()
    {
        var fuente = Fuente("Controllers/StockController.cs");
        var metodo = fuente[fuente.IndexOf("private async Task<IActionResult> ConsultaTecnica")..fuente.IndexOf("private async Task CargarProveedores")];
        Assert.Contains("ObtenerRepuestosTecnicosAsync", metodo);
        Assert.Contains("View(\"ConsultaTecnica\", resultado.Data)", metodo);
        Assert.DoesNotContain("Json(", metodo);
        Assert.Contains("return Forbid()", metodo);
        Assert.Contains("return NotFound()", metodo);
        var vista = Fuente("Views/Stock/ConsultaTecnica.cshtml");
        Assert.Contains("RepuestoTecnicoDto", vista);
        Assert.Contains("Model.Count == 0", vista);
        Assert.DoesNotContain("Precio", vista);
        Assert.DoesNotContain("Proveedor", vista);
    }

    [Theory]
    [InlineData("Usuario", "USUARIO")]
    [InlineData("Proveedor", "PROVEEDOR")]
    [InlineData("Stock", "STOCK")]
    public void BotonesAdministrativos_ExigenSuPatente(string modulo, string patente)
    {
        var vista = Fuente($"Views/{modulo}/Index.cshtml");
        Assert.Contains($"\"{patente}_CREAR\"", vista);
        Assert.Contains($"\"{patente}_MODIFICAR\"", vista);
        Assert.Contains($"\"{patente}_{(modulo == "Stock" ? "MODIFICAR" : "DESACTIVAR")}\"", vista);
        foreach (var (condicion, accion) in new[] { ("puedeCrear", "Crear"), ("puedeEditar", "Editar"), ("puedeCambiarEstado", "CambiarEstado") })
            Assert.Matches($"@if \\({condicion}[^\\r\\n]*\\)\\s*\\{{\\s*<(a|form) asp-action=\"{accion}\"", vista);
    }

    [Fact]
    public void Stock_InactivoNoTieneEnlacesIncompatibles()
    {
        var vista = Fuente("Views/Stock/Index.cshtml");
        Assert.Matches("@if \\(item.Activo\\)\\s*\\{\\s*<a asp-action=\"Detalle\"", vista);
        Assert.Matches("@if \\(puedeEditar && item.Activo\\)\\s*\\{\\s*<a asp-action=\"Editar\"", vista);
        Assert.Contains("@if (puedeCambiarEstado)", vista); // conserva reactivación
    }

    [Fact]
    public void Cliente_AccesosDependenDeCuentaHabilitada_PasswordNoDuplicada()
    {
        var layout = Fuente("Views/Shared/_Layout.cshtml");
        Assert.Contains("@if (cuentaVisible.EsClienteHabilitado &&", layout);
        var portal = Fuente("Views/PortalCliente/Index.cshtml");
        Assert.True(portal.IndexOf("@if (cuenta.EsClienteHabilitado)") < portal.IndexOf("asp-action=\"MisDatos\""));
        Assert.DoesNotContain("SolicitarEnlace", portal);
        Assert.Single(Regex.Matches(layout, "asp-action=\"SolicitarEnlace\""));
        var cuenta = Fuente("Services/PresentacionCuentaService.cs");
        Assert.Contains("ExclusivamenteClienteAsync", cuenta);
        Assert.Contains("habilitado.EstaHabilitadoAsync", cuenta);
    }

    [Fact]
    public void Ot_ConservaUnAccesoPorRecurso()
    {
        var vista = Fuente("Views/OrdenTrabajo/Detalle.cshtml");
        Assert.Single(Regex.Matches(vista, "asp-controller=\"Presupuesto\"\\s+asp-action=\"Detalle\""));
        Assert.Single(Regex.Matches(vista, "asp-controller=\"Factura\"\\s+asp-action=\"Detalle\""));
    }

    [Fact]
    public void Inicio_YErrorUsanNavegacionLocalSinHtmlCrudo()
    {
        foreach (var archivo in new[] { "Views/Account/CompletarDatosExito.cshtml", "Views/Shared/_Layout.cshtml" })
            Assert.Contains("asp-controller=\"Account\" asp-action=\"Inicio\"", Fuente(archivo));
        var error = Fuente("Views/Presupuesto/ErrorOperacion.cshtml");
        Assert.Contains("@Model", error);
        Assert.DoesNotContain("Html.Raw", error);
        Assert.DoesNotContain("history.back", error);
        Assert.Contains("CLIENTE_PRESUPUESTO_VER", error);
        Assert.Contains("ORDEN_VER", error);
    }
}
