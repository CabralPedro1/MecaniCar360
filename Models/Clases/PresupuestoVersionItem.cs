namespace MecaniCar360.Models
{
    public class PresupuestoVersionItem
    {
        public int Id { get; set; }
        public int PresupuestoVersionId { get; set; }
        public PresupuestoVersion PresupuestoVersion { get; set; } = null!;
        public string Descripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int? RepuestoId { get; set; }
        public Repuesto? Repuesto { get; set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}
