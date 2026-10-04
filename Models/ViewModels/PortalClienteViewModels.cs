using System.ComponentModel.DataAnnotations;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Models.ViewModels;

public sealed class OrdenPropiaViewModel
{
    public OrdenTrabajo Orden { get; init; } = null!;
    public PresupuestoVersion? Presupuesto { get; init; }
    public int? FacturaId { get; init; }
    public Diagnostico? Diagnostico { get; init; }
}

public sealed class PresupuestoPropioResumenViewModel
{
    public int OrdenTrabajoId { get; init; }
    public string Patente { get; init; } = "";
    public int NumeroVersion { get; init; }
    public DateTime FechaEnvio { get; init; }
    public decimal Total { get; init; }
    public EstadoPresupuestoVersion Decision { get; init; }
}

public sealed class NuevoTurnoPropioViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int? VehiculoId { get; set; }
    [EnumDataType(typeof(TipoTurno))]
    public TipoTurno Tipo { get; set; }
    [Required, Display(Name = "Fecha y hora")]
    public DateTime? FechaInicio { get; set; }
    [Required, StringLength(500)]
    public string Motivo { get; set; } = "";
    [StringLength(1000)]
    public string? Observaciones { get; set; }
}
