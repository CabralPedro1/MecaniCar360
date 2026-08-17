using MecaniCar360.Models.Enums;

namespace MecaniCar360.Models
{
    public class MovimientoStock
    {
        public int Id { get; set; }

        public int RepuestoId { get; set; }
        public Repuesto Repuesto { get; set; }

        // Solo se utiliza en ingresos de mercadería
        public int? ProveedorRepuestoId { get; set; }
        public ProveedorRepuesto? ProveedorRepuesto { get; set; }

        // Positivo = ingreso | Negativo = egreso
        public int Cantidad { get; set; }

        public DateTime Fecha { get; set; }

        public TipoMovimientoStock Tipo { get; set; }

        public int RealizadoPorUsuarioId { get; set; }
        public Usuario RealizadoPorUsuario { get; set; }

        public int? OrdenTrabajoId { get; set; }
        public OrdenTrabajo? OrdenTrabajo { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string? Observaciones { get; set; }
    }
}