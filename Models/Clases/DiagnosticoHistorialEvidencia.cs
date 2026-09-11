namespace MecaniCar360.Models
{
    public class DiagnosticoHistorialEvidencia
    {
        public int DiagnosticoHistorialId { get; set; }
        public DiagnosticoHistorial DiagnosticoHistorial { get; set; } = null!;
        public int EvidenciaTrabajoId { get; set; }
        public EvidenciaTrabajo EvidenciaTrabajo { get; set; } = null!;
    }
}
