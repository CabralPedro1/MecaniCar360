using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Repuesto
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string SKU { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(100)]
        public string? Marca { get; set; }

        [MaxLength(100)]
        public string? Modelo { get; set; }

        [MaxLength(300)]
        public string? Compatibilidad { get; set; }

        [Required]
        public decimal PrecioVenta { get; set; }

        public int StockActual { get; set; }

        public int StockMinimo { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public List<ProveedorRepuesto> Proveedores { get; set; } = new();

        public List<MovimientoStock> Movimientos { get; set; } = new();

        public List<PresupuestoItem> PresupuestoItems { get; set; } = new();
    }

    public class Proveedor
    {
        public int Id { get; set; }

        public string Nombre { get; set; }

        public string Apellido { get; set; }

        public string Telefono { get; set; }

        public string Email { get; set; }

        public DateTime FechaCreacion { get; set; }

        public bool Activo { get; set; } = true;

        public List<ProveedorRepuesto> Repuestos { get; set; } = new();
    }

    public class ProveedorRepuesto
    {
        public int Id { get; set; }

        [Required]
        public int ProveedorId { get; set; }
        public Proveedor Proveedor { get; set; }

        [Required]
        public int RepuestoId { get; set; }
        public Repuesto Repuesto { get; set; }

        [Required]
        public decimal PrecioCompraActual { get; set; }

        [MaxLength(50)]
        public string? CodigoProveedor { get; set; }

        public bool Principal { get; set; } = false;
    }
}
