using MecaniCar360.Models;
using MecaniCar360.Models.Enums;

public class MovimientoVehiculo
{
    public int Id { get; set; }

    public int IngresoVehiculoId { get; set; }
    public IngresoVehiculo IngresoVehiculo { get; set; } = null!;

    public EstadoIngreso Estado { get; set; }

    public DateTime Fecha { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string? Observaciones { get; set; }
}