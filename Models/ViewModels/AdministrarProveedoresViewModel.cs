using Microsoft.AspNetCore.Mvc.Rendering;

namespace MecaniCar360.Models.ViewModels
{
    public class AdministrarProveedoresViewModel
    {
        public int RepuestoId { get; set; }
        public Repuesto Repuesto { get; set; } = null!;

        public List<ProveedorRepuesto> Proveedores { get; set; } = new();

        public List<SelectListItem> ProveedoresDisponibles { get; set; } = new();

        public int ProveedorId { get; set; }

        public decimal PrecioCompra { get; set; }

        public string? CodigoProveedor { get; set; }

        public bool Principal { get; set; }
    }
}