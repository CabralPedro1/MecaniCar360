namespace MecaniCar360.Models
{
    public class PresupuestoVersionEvidencia
    {
        public int PresupuestoVersionId { get; set; }
        public PresupuestoVersion PresupuestoVersion { get; set; } = null!;
        public int EvidenciaTrabajoId { get; set; }
        public EvidenciaTrabajo EvidenciaTrabajo { get; set; } = null!;
    }
}
