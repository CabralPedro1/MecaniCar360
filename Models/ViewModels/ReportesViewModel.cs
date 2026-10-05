using System.ComponentModel.DataAnnotations;
using MecaniCar360.Models.Enums;
namespace MecaniCar360.Models.ViewModels;

public sealed class ReporteFiltro : IValidatableObject
{
    [Required, DataType(DataType.Date)] public DateTime? Desde { get; set; }
    [Required, DataType(DataType.Date)] public DateTime? Hasta { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Desde.HasValue || !Hasta.HasValue) yield break;
        if (Desde.Value.Date > Hasta.Value.Date) yield return new("Desde no puede superar Hasta.");
        if ((Hasta.Value.Date - Desde.Value.Date).TotalDays > 366) yield return new("El período máximo es 367 días inclusive.");
        if (Hasta.Value.Date == DateTime.MaxValue.Date) yield return new("Fecha fuera del rango admitido.");
    }
}
public sealed class ReportesViewModel
{
    public ReporteFiltro Filtro { get; set; } = new();
    public int Ingresadas { get; set; }
    public int Finalizadas { get; set; }
    public int Entregadas { get; set; }
    public int EnReparacion { get; set; }
    public int Rechazadas { get; set; }
    public decimal PorcentajeFinalizadas => Ingresadas == 0 ? 0 : 100m * Finalizadas / Ingresadas;
    public decimal PorcentajeEntregadas => Ingresadas == 0 ? 0 : 100m * Entregadas / Ingresadas;
    public List<ReporteEstado> Estados { get; set; } = new();
    public int Facturas { get; set; }
    public decimal Facturado { get; set; }
    public decimal Cobrado { get; set; }
    public decimal Saldo { get; set; }
    public List<ReporteConcepto> Conceptos { get; set; } = new();
    public string Resumen => $"Se registraron {Ingresadas} órdenes en el período; a la fecha de consulta, {Finalizadas} tienen finalización registrada y {Entregadas} están entregadas.";
}
public sealed record ReporteEstado(EstadoOrden Estado, int Cantidad);
public sealed record ReporteConcepto(string Descripcion, int Cantidad, decimal Importe)
{
    public decimal Porcentaje { get; set; }
}
