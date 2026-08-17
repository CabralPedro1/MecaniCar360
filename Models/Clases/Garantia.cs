namespace MecaniCar360.Models
{
    public class Garantia
    {
        public int Id { get; set; }

        public int OrdenTrabajoId { get; set; }
        public OrdenTrabajo OrdenTrabajo { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        public bool Activa { get; set; } = true;

        // 🔥 QUIÉN LA CREÓ
        public int CreadaPorUsuarioId { get; set; }
        public Usuario CreadaPorUsuario { get; set; }

        public List<GarantiaItem> Items { get; set; } = new();
    }

    public class GarantiaItem
    {
        public int Id { get; set; }

        public int GarantiaId { get; set; }
        public Garantia Garantia { get; set; }

        public int PresupuestoItemId { get; set; }
        public PresupuestoItem PresupuestoItem { get; set; }

        public int MesesGarantia { get; set; }

        public string? Observaciones { get; set; }
    }
}
