using MecaniCar360.Models.Enums;

namespace MecaniCar360.Models.ViewModels;

public sealed class OrdenTrabajoDetalleViewModel
{
    public int Id { get; init; }
    public EstadoOrden EstadoActual { get; init; }
    public NivelUrgencia Urgencia { get; init; }
    public DateTime FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }
    public decimal? CostoDiagnostico { get; init; }
    public string? Observaciones { get; init; }
    public string ClienteNombre { get; init; } = "";
    public string ClienteApellido { get; init; } = "";
    public string VehiculoMarca { get; init; } = "";
    public string VehiculoModelo { get; init; } = "";
    public string VehiculoPatente { get; init; } = "";
    public PersonaNombreDetalle? Mecanico { get; init; }
    public List<EstadoOrdenDetalle> HistorialEstados { get; init; } = new();
    public DiagnosticoOrdenDetalle? Diagnostico { get; init; }
    public PresupuestoOrdenDetalle? Presupuesto { get; init; }
    public FacturaOrdenResumen? FacturaResumen { get; init; }
    public bool PuedeVerFactura { get; init; }
    public bool PuedeEntregar { get; init; }
    public bool PuedeEmitirFactura { get; init; }
    public bool PuedeEditarCosto { get; init; }
}
public sealed record PersonaNombreDetalle(string Nombre, string Apellido);
public sealed record EstadoOrdenDetalle(DateTime Fecha, EstadoOrden Estado, PersonaNombreDetalle? Mecanico);
public sealed record DiagnosticoOrdenDetalle(string DescripcionActual, DateTime FechaUltimaModificacion);
public sealed record PresupuestoOrdenDetalle(EstadoPresupuesto Estado, decimal Total, DateTime FechaUltimaModificacion, string? MotivoRechazo);
public sealed class FacturaOrdenResumen
{
    public string NumeroFactura { get; init; } = "";
    public EstadoFactura Estado { get; init; }
    public decimal Total { get; init; }
}
