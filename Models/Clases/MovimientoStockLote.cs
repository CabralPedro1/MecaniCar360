namespace MecaniCar360.Models
{
    public class MovimientoStockLote
    {
        public int MovimientoStockId { get; set; }
        public MovimientoStock MovimientoStock { get; set; } = null!;
        public int LoteRepuestoId { get; set; }
        public LoteRepuesto LoteRepuesto { get; set; } = null!;
        // Magnitud positiva; el signo permanece en MovimientoStock.Cantidad.
        public int Cantidad { get; set; }
    }
}
