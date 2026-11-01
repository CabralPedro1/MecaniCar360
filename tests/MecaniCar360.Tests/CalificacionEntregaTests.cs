using System.Reflection;
using MecaniCar360.Controllers;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.Enums;
using MecaniCar360.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Tests;

// Reglas ejecutadas en memoria y estructura transaccional; no acredita persistencia ni carreras SQL.
public class CalificacionEntregaTests
{
    private static object? Invocar(Type tipo, string metodo, params object?[] args) =>
        tipo.GetMethod(metodo, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args);

    [Theory]
    [InlineData("CLIENTE", true, false, false, true)]
    [InlineData("ADMIN", true, false, false, false)]
    [InlineData("CAJA", true, false, false, false)]
    [InlineData("MECANICO", true, false, false, false)]
    [InlineData("STOCK", true, false, false, false)]
    [InlineData("CLIENTE", false, false, false, false)]
    [InlineData("CLIENTE", true, true, false, false)]
    [InlineData("CLIENTE", true, false, true, false)]
    public void FiltroDeCliente_ExcluyeInternosInactivosYPendientes(
        string rol, bool activo, bool pendiente, bool mixto, bool esperado)
    {
        var persona = new Persona { Activo = activo, Roles = new() {
            new() { Rol = new Rol { Nombre = rol, Activo = true } } } };
        if (mixto) persona.Roles.Add(new() { Rol = new Rol { Nombre = "ADMIN", Activo = true } });
        var usuarios = new[] { new Usuario { Activo = activo, PrimerLogin = pendiente, Persona = persona } }.AsQueryable();
        var resultado = Assert.IsAssignableFrom<IQueryable<Usuario>>(
            Invocar(typeof(CalificacionTrabajoService), "ClientesElegibles", usuarios));
        Assert.Equal(esperado, resultado.Any());
    }

    [Theory]
    [InlineData(EstadoOrden.Entregado, 7, 7, true, false, true)]
    [InlineData(EstadoOrden.Finalizado, 7, 7, true, false, false)]
    [InlineData(EstadoOrden.Entregado, 7, 8, true, false, false)]
    [InlineData(EstadoOrden.Entregado, 7, 7, false, false, false)]
    [InlineData(EstadoOrden.Entregado, 7, 7, true, true, false)]
    public void ReglaDeOrden_ExigeEntregaPropiedadTitularidadYUnicidad(
        EstadoOrden estado, int propietario, int actor, bool titular, bool existente, bool permitido)
    {
        var error = Invocar(typeof(CalificacionTrabajoService), "ValidarOrden",
            estado, propietario, actor, titular, existente);
        Assert.Equal(permitido, error == null);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(6, 0)]
    [InlineData(3, 1001)]
    public async Task EntradaInvalida_RechazaAntesDeAccederAPersistencia(int puntuacion, int longitud)
    {
        var servicio = new CalificacionTrabajoService(null!, null!);
        var resultado = await servicio.CrearAsync(1, 1, puntuacion, new string('a', longitud));
        Assert.False(resultado.Exitoso);
    }

    [Theory]
    [InlineData(1, null)]
    [InlineData(5, "")]
    [InlineData(3, "Comentario")]
    public void EntradaValida_ConservaComentarioOpcional(int puntos, string? comentario) =>
        Assert.Null(Invocar(typeof(CalificacionTrabajoService), "ValidarEntrada", puntos, comentario));

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void Solicitud_RespuestaIndependienteDeLectura(bool leida, bool calificada, bool respondida)
    {
        var filas = Solicitudes(leida, calificada, 7, EstadoOrden.Entregado);
        var fila = Assert.Single(filas);
        Assert.Equal(respondida, fila.GetType().GetProperty("Respondida")!.GetValue(fila));
    }

    [Theory]
    [InlineData(8, EstadoOrden.Entregado)]
    [InlineData(7, EstadoOrden.Finalizado)]
    public void SolicitudAjenaONoEntregada_NoSeOfrece(int destinatario, EstadoOrden estado) =>
        Assert.Empty(Solicitudes(false, false, destinatario, estado));

    private static IEnumerable<object> Solicitudes(bool leida, bool calificada, int destinatario, EstadoOrden estado)
    {
        var notificaciones = new[] { new Notificacion { Id = 11, PersonaId = destinatario,
            Titulo = "Título libre", TipoRecurso = TipoRecursoNotificacion.SolicitudCalificacion,
            RecursoId = 10, Leida = leida } }.AsQueryable();
        var ordenes = new[] { new OrdenTrabajo { Id = 10, EstadoActual = estado,
            IngresoVehiculo = new IngresoVehiculo { Turno = new Turno { ClienteId = 7 } },
            Calificacion = calificada ? new CalificacionTrabajo() : null } }.AsQueryable();
        return ((System.Collections.IEnumerable)Invocar(typeof(NotificacionService),
            "SolicitudesDeCalificacion", notificaciones, ordenes, 7)!).Cast<object>().ToArray();
    }

    [Theory]
    [InlineData(TipoRecursoNotificacion.SolicitudCalificacion, "Vehículo entregado", true)]
    [InlineData(TipoRecursoNotificacion.SolicitudCalificacion, "Gracias por visitarnos", true)]
    [InlineData(TipoRecursoNotificacion.SolicitudCalificacion, "", true)]
    [InlineData(TipoRecursoNotificacion.OrdenPropia, "Vehículo entregado", true)]
    [InlineData(TipoRecursoNotificacion.OrdenPropia, "Vehículo listo para retirar", false)]
    [InlineData(TipoRecursoNotificacion.OrdenTrabajo, "Vehículo entregado", false)]
    [InlineData(TipoRecursoNotificacion.FacturaPropia, "Vehículo entregado", false)]
    [InlineData(TipoRecursoNotificacion.GarantiaPropia, "Vehículo entregado", false)]
    [InlineData(TipoRecursoNotificacion.PresupuestoPropio, "Vehículo entregado", false)]
    [InlineData(TipoRecursoNotificacion.Turno, "Vehículo entregado", false)]
    [InlineData(TipoRecursoNotificacion.Repuesto, "Vehículo entregado", false)]
    public void IdentificadorEstable_YCompatibilidadHistoricaAcotada(
        TipoRecursoNotificacion tipo, string titulo, bool esperado)
    {
        var avisos = new[] { new Notificacion { PersonaId = 7, RecursoId = 10,
            TipoRecurso = tipo, Titulo = titulo } }.AsQueryable();
        var resultado = (IQueryable<Notificacion>)Invocar(typeof(NotificacionService),
            "EntregasPropias", avisos, 7)!;
        Assert.Equal(esperado, resultado.Any());
    }

    [Fact]
    public void EnumConservaValoresPersistidos_YEntregaEmiteUnaSolicitud()
    {
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, new[] {
            (int)TipoRecursoNotificacion.OrdenPropia, (int)TipoRecursoNotificacion.PresupuestoPropio,
            (int)TipoRecursoNotificacion.FacturaPropia, (int)TipoRecursoNotificacion.GarantiaPropia,
            (int)TipoRecursoNotificacion.OrdenTrabajo, (int)TipoRecursoNotificacion.Turno,
            (int)TipoRecursoNotificacion.Repuesto });
        Assert.Equal(8, (int)TipoRecursoNotificacion.SolicitudCalificacion);
        var entrega = Fuente("Services/OrdenTrabajoService.cs");
        Assert.Equal(1, entrega.Split("TipoRecursoNotificacion.SolicitudCalificacion").Length - 1);
        Assert.Contains("""ClienteAsync(orden.Id, "Vehículo entregado", TipoRecursoNotificacion.SolicitudCalificacion, orden.Id)""", entrega);
    }

    [Fact]
    public void ConsultasSonTraducibles_SinAbrirConexion()
    {
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=SoloMetadatos;Integrated Security=true;Connect Timeout=1").Options);
        var clientes = (IQueryable<Usuario>)Invocar(typeof(CalificacionTrabajoService),
            "ClientesElegibles", db.Usuarios.AsNoTracking())!;
        Assert.Contains("NOT EXISTS", clientes.ToQueryString());
        var solicitudes = (IQueryable)Invocar(typeof(NotificacionService),
            "SolicitudesDeCalificacion", db.Notificaciones.AsNoTracking(), db.OrdenesTrabajo.AsNoTracking(), 7)!;
        Assert.Contains("Calificaciones", solicitudes.ToQueryString());
        var indice = db.Model.FindEntityType(typeof(CalificacionTrabajo))!.GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == "OrdenTrabajoId");
        Assert.True(indice.IsUnique);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Fact]
    public void Creacion_ProteccionAntesDeAutorizacionYLectura_SinExitoAntesDelCommit()
    {
        var fuente = Fuente("Services/CalificacionTrabajoService.cs");
        var crear = fuente[fuente.IndexOf("public async Task<ServiceResult> CrearAsync")..];
        var posicion = 0;
        foreach (var paso in new[] { "BeginTransactionAsync(System.Data.IsolationLevel.Serializable)",
            "ClienteAutorizadoAsync", "ValidarOrdenAsync", "_context.Calificaciones.Add",
            "SaveChangesAsync()", "tx.CommitAsync()", "return ServiceResult.Ok" })
        {
            var siguiente = crear.IndexOf(paso, posicion, StringComparison.Ordinal);
            Assert.True(siguiente >= posicion, paso);
            posicion = siguiente + paso.Length;
        }
        Assert.Contains("tx.RollbackAsync()", crear);
        Assert.Contains("ClienteHabilitadoService", fuente);
        Assert.Contains("_context.OrdenesTrabajo.AsNoTracking()", fuente);
        Assert.Contains("if (error != null) return ServiceResult.Error(error);", crear);
    }

    [Fact]
    public void Notificacion_ConservaOwnershipYEntregaUnica_EstrellasConservanPost()
    {
        var controlador = Fuente("Controllers/NotificacionController.cs");
        Assert.True(controlador.IndexOf("ObtenerPropiaAsync") < controlador.IndexOf("ContainsKey(n.Id)"));
        Assert.Contains("if (!r.Exitoso) return NotFound();", controlador);
        Assert.Contains("""RedirectToAction("Detalle", "Calificacion", new { ordenTrabajoId = n.RecursoId })""", controlador);
        var entrega = Fuente("Services/OrdenTrabajoService.cs");
        Assert.Equal(1, entrega.Split("\"Vehículo entregado\"").Length - 1);
        var vista = Fuente("Views/Calificacion/Detalle.cshtml");
        Assert.Contains("type=\"radio\"", vista);
        Assert.Contains("<fieldset", vista);
        Assert.Contains("<legend", vista);
        Assert.Contains("asp-action=\"Crear\" method=\"post\"", vista);
        Assert.Contains("@Html.AntiForgeryToken()", vista);
    }

    internal static string Fuente(string ruta)
    {
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz != null && !File.Exists(Path.Combine(raiz.FullName, "MecaniCar360.csproj"))) raiz = raiz.Parent;
        return File.ReadAllText(Path.Combine(raiz!.FullName, ruta));
    }
}
