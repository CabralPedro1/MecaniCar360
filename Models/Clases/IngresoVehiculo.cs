namespace MecaniCar360.Models
{
    public class IngresoVehiculo
    {
        public int Id { get; set; }

        public int TurnoId { get; set; }
        public Turno Turno { get; set; } = null!;

        public DateTime FechaIngreso { get; set; }

        public DateTime? FechaEgreso { get; set; }

        public bool ClienteEspera { get; set; }

        public string? ObservacionesRecepcion { get; set; }
    }
}