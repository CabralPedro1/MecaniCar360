namespace MecaniCar360.Models
{
    public class LoteRepuesto
    {
        public int Id { get; set; }
        public string CodigoLote { get; set; } = string.Empty;
        public int RepuestoId { get; set; }
        public Repuesto Repuesto { get; set; } = null!;
        public int ProveedorRepuestoId { get; set; }
        public ProveedorRepuesto ProveedorRepuesto { get; set; } = null!;
        public int CantidadIngresada { get; set; }
        public int CantidadDisponible { get; set; }
        public decimal PrecioCompra { get; set; }
        public DateTime FechaIngreso { get; set; }
        public ICollection<MovimientoStockLote> Movimientos { get; set; } = new List<MovimientoStockLote>();
    }
}
