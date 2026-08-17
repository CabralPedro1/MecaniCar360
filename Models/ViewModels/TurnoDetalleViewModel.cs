using MecaniCar360.Models;

namespace MecaniCar360.Models.ViewModels
{
    public class TurnoDetalleViewModel
    {
        public Turno Turno { get; set; } = null!;

        public IngresoVehiculo? IngresoVehiculo { get; set; }

        public OrdenTrabajo? OrdenTrabajo { get; set; }

        public List<TurnoEstadoHistorial> Historial { get; set; } = new();
    }
}