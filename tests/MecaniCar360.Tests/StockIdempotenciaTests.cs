using System.Globalization;
using System.Reflection;
using MecaniCar360.Data;
using MecaniCar360.Data.Integridad;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Tests;

public class StockIdempotenciaTests
{
    [Fact]
    public void Huella_ConservaContratoJsonVersionUno()
    {
        const string json = "{\"Version\":1,\"Tipo\":\"Ingreso\",\"RepuestoId\":1,\"Cantidad\":2,\"Observaciones\":\"caf\\u00E9\",\"Proveedor\":3,\"Precio\":\"12.50\",\"Orden\":null,\"Lote\":null}";
        var datos = Datos(); datos[3] = " café ";
        Assert.Equal(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)), Huella(datos));
    }

    [Theory]
    [InlineData("RegistrarIngreso")]
    [InlineData("RegistrarSalida")]
    [InlineData("RegistrarAjuste")]
    public void Formularios_ConservanClaveRechazadaYGeneranClavesIndependientes(string accion)
    {
        var controller = new MecaniCar360.Controllers.StockController(null!, null!, null!, null!);
        var preparar = controller.GetType().GetMethod("PrepararClavesMovimiento", BindingFlags.Instance | BindingFlags.NonPublic)!;
        preparar.Invoke(controller, null);
        var acciones = new[] { "RegistrarIngreso", "RegistrarSalida", "RegistrarAjuste" };
        var claves = acciones.Select(a => Guid.Parse((string)controller.ViewData["Clave" + a]!)).ToArray();
        Assert.Equal(3, claves.Distinct().Count());
        Assert.DoesNotContain(Guid.Empty, claves);
        var clave = (string)controller.ViewData["Clave" + accion]!;
        controller.ViewData["MovimientoRechazado"] = accion;
        controller.ModelState.SetModelValue("claveOperacion", clave, clave);
        controller.ModelState.AddModelError("cantidad", "Cantidad inválida");
        preparar.Invoke(controller, null);
        Assert.Equal(clave, controller.ViewData["Clave" + accion]);
        preparar.Invoke(controller, null);
        Assert.Equal(clave, controller.ViewData["Clave" + accion]);
        controller.ViewData.Remove("MovimientoRechazado");
        preparar.Invoke(controller, null);
        Assert.NotEqual(clave, controller.ViewData["Clave" + accion]);
    }

    [Fact]
    public void Migracion_SoloCreaTablaTecnicaEIndices()
    {
        var migracion = new MecaniCar360.Migrations.AgregarIdempotenciaMovimientosStock();
        var operaciones = migracion.UpOperations;
        var tabla = Assert.Single(operaciones.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation>());
        Assert.Equal("OperacionesStockIdempotentes", tabla.Name);
        Assert.Equal(2, tabla.ForeignKeys.Count);
        Assert.All(tabla.ForeignKeys, fk => Assert.Equal(Microsoft.EntityFrameworkCore.Migrations.ReferentialAction.NoAction, fk.OnDelete));
        Assert.Equal(2, operaciones.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation>().Count());
        Assert.Equal(3, operaciones.Count);
    }

    private static byte[] Huella(params object?[] campos) => (byte[])typeof(StockService)
        .GetMethod("HuellaMovimiento", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, campos)!;

    private static object?[] Datos() => new object?[] { "Ingreso", 1, 2, " motivo ", 3, 12.50m, null, null };

    [Fact]
    public void Huella_EstableEntreCulturasYEscalasDecimales()
    {
        var anterior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-AR");
            var primera = Huella(Datos());
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var datos = Datos(); datos[5] = 12.5m; datos[3] = "motivo";
            Assert.Equal(primera, Huella(datos));
            Assert.Equal(32, primera.Length);
        }
        finally { CultureInfo.CurrentCulture = anterior; }
    }

    [Theory]
    [InlineData(0, "Salida")]
    [InlineData(1, 4)]
    [InlineData(2, 4)]
    [InlineData(3, "otro motivo")]
    [InlineData(4, 4)]
    [InlineData(6, 4)]
    [InlineData(7, 4)]
    public void Huella_DistingueParametros(int indice, object valor)
    {
        var datos = Datos(); var original = Huella(datos); datos[indice] = valor;
        Assert.False(original.SequenceEqual(Huella(datos)));
    }

    [Fact]
    public void Huella_DistinguePrecioYNull()
    {
        var datos = Datos(); var original = Huella(datos); datos[5] = 12.51m;
        Assert.False(original.SequenceEqual(Huella(datos)));
        datos = Datos(); datos[3] = null; var nula = Huella(datos); datos[3] = "";
        Assert.False(nula.SequenceEqual(Huella(datos)));
    }

    [Theory]
    [InlineData(1, "Ingreso", false, true)]
    [InlineData(2, "Ingreso", false, false)]
    [InlineData(1, "Salida", false, false)]
    [InlineData(1, "Ingreso", true, false)]
    public void Repeticion_ExigeMismoActorOperacionYDatos(int actor, string tipo, bool cambiar, bool exito)
    {
        var huella = Huella(Datos());
        var registro = new OperacionStockIdempotente
        {
            UsuarioId = 1, TipoOperacion = "Ingreso", HuellaSolicitud = huella,
            MovimientoStockId = 15, MensajeResultado = "Ingreso registrado. Lote original."
        };
        var solicitud = huella.ToArray(); if (cambiar) solicitud[0] ^= 1;
        var resultado = (ServiceResult)typeof(StockService).GetMethod("ResultadoRepetido",
            BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { registro, actor, tipo, solicitud })!;
        Assert.Equal(exito, resultado.Exitoso);
        if (exito) Assert.Equal(registro.MensajeResultado, resultado.Mensaje);
        else Assert.DoesNotContain("Lote original", resultado.Mensaje);
        Assert.Equal(15, registro.MovimientoStockId);
        Assert.Equal(huella, registro.HuellaSolicitud);
    }

    [Fact]
    public async Task ClaveVacia_RechazadaAntesDeTransaccionODependencias()
    {
        var servicio = new StockService(null!, null!, null!, null!);
        var invocada = false;
        Func<OperacionStockIdempotente, Task<ServiceResult>> operacion = _ =>
        { invocada = true; return Task.FromResult(ServiceResult.Ok()); };
        var tarea = (Task<ServiceResult>)typeof(StockService).GetMethod("EjecutarMovimientoAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(servicio,
                new object[] { Guid.Empty, 1, "Ingreso", Huella(Datos()), operacion })!;
        Assert.False((await tarea).Exitoso);
        Assert.False(invocada);
    }

    [Fact]
    public void Modelo_ClaveYMovimientoUnicos_SinCascadasNiAmpliacionDVH()
    {
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=SoloMetadatos;Integrated Security=true").Options);
        var entidad = db.Model.FindEntityType(typeof(OperacionStockIdempotente))!;
        Assert.Equal("Clave", Assert.Single(entidad.FindPrimaryKey()!.Properties).Name);
        Assert.Contains(entidad.GetIndexes(), i => i.IsUnique && i.Properties.Single().Name == "MovimientoStockId");
        Assert.All(entidad.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior));
        Assert.Equal("binary(32)", entidad.FindProperty("HuellaSolicitud")!.GetColumnType());
        Assert.Null(entidad.FindProperty("DVH"));
        Assert.Equal(12, RegistroEntidadesProtegidas.Todas.Count);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }
}
