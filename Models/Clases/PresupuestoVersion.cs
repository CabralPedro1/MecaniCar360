using MecaniCar360.Models.Enums;

namespace MecaniCar360.Models
{
    public class PresupuestoVersion
    {
        public int Id { get; set; }
        public int PresupuestoId { get; set; }
        public Presupuesto Presupuesto { get; set; } = null!;
        public int NumeroVersion { get; set; }
        public decimal Total { get; set; }
        public EstadoPresupuestoVersion Decision { get; set; } = EstadoPresupuestoVersion.Pendiente;
        public DateTime FechaEnvio { get; set; }
        public int EnviadaPorUsuarioId { get; set; }
        public Usuario EnviadaPorUsuario { get; set; } = null!;
        public DateTime? FechaDecision { get; set; }
        public int? DecididaPorUsuarioId { get; set; }
        public Usuario? DecididaPorUsuario { get; set; }
        public string? MotivoRechazo { get; set; }
        public ICollection<PresupuestoVersionItem> Items { get; set; } = new List<PresupuestoVersionItem>();
        public ICollection<PresupuestoVersionEvidencia> Evidencias { get; set; } = new List<PresupuestoVersionEvidencia>();
    }
}
