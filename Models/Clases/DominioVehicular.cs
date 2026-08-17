namespace MecaniCar360.Models
{
    public class DominioVehicular
    {
        public int Id { get; set; }

        public int VehiculoId { get; set; }
        public Vehiculo Vehiculo { get; set; }

        public int PersonaId { get; set; }
        public Persona Persona { get; set; }   // Titular del vehículo

        public DateTime FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }
}
