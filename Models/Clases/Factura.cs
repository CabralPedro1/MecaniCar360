using MecaniCar360.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Factura
    {
        public int Id { get; set; }

        // =====================================
        // ORDEN DE TRABAJO
        // =====================================

        public int OrdenTrabajoId { get; set; }
        public OrdenTrabajo OrdenTrabajo { get; set; } = null!;

        // =====================================
        // PRESUPUESTO DE ORIGEN
        // =====================================

        public int? PresupuestoOrigenId { get; set; }
        public Presupuesto? PresupuestoOrigen { get; set; } = null!;

        // =====================================
        // ESTADO
        // =====================================

        public EstadoFactura Estado { get; set; }
            = EstadoFactura.Emitida;

        // =====================================
        // DATOS DE FACTURA
        // =====================================

        public decimal Total { get; set; }

        public DateTime FechaEmision { get; set; }
            = DateTime.Now;

        [Required]
        [MaxLength(50)]
        public string NumeroFactura { get; set; }
            = string.Empty;

        public string? Observaciones { get; set; }

        // =====================================
        // MEDIOS DE PAGO
        // =====================================

        public string? LinkPago { get; set; }

        public string? QRPago { get; set; }

        // =====================================
        // ITEMS
        // =====================================

        public List<FacturaItem> Items { get; set; }
            = new();

        // =====================================
        // PAGOS
        // =====================================

        public List<Pago> Pagos { get; set; }
            = new();
    }


    public class FacturaItem
    {
        public int Id { get; set; }

        public string Descripcion { get; set; }
            = string.Empty;

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public int FacturaId { get; set; }

        public Factura Factura { get; set; }
            = null!;

        public decimal Subtotal =>
            Cantidad * PrecioUnitario;
    }
}