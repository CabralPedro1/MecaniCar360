namespace MecaniCar360.Models
{
    public class Diagnostico
    {
        public int Id { get; set; }

        public int OrdenTrabajoId { get; set; }
        public OrdenTrabajo OrdenTrabajo { get; set; }

        public string DescripcionActual { get; set; }

        public DateTime FechaUltimaModificacion { get; set; }

        public List<DiagnosticoHistorial> Historial { get; set; } = new();
    }

    public class DiagnosticoHistorial
    {
        public int Id { get; set; }

        public int DiagnosticoId { get; set; }
        public Diagnostico Diagnostico { get; set; }

        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }

        public int? MecanicoId { get; set; }
        public Persona? Mecanico { get; set; } 
    }
}