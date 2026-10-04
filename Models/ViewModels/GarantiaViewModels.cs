namespace MecaniCar360.Models.ViewModels;

public sealed class GarantiaDetalleViewModel
{
    public Garantia Garantia { get; init; } = null!;
    public string Estado { get; init; } = "";
}

public sealed class CrearGarantiaViewModel
{
    public int OrdenTrabajoId { get; init; }
    public DateTime FechaInicio { get; init; }
    public List<FacturaItem> Items { get; init; } = new();
}
