using MecaniCar360.Models.Enums;

namespace MecaniCar360.Models
{
    public class Presupuesto
    {
        public int Id { get; set; }

        public int OrdenTrabajoId { get; set; }
        public OrdenTrabajo OrdenTrabajo { get; set; }

        public int? MecanicoId { get; set; }
        public Persona? Mecanico { get; set; }

        public EstadoPresupuesto Estado { get; set; } = EstadoPresupuesto.Pendiente;

        public decimal Total { get; set; }

        public DateTime FechaUltimaModificacion { get; set; }

        public List<PresupuestoItem> Items { get; set; } = new();
        public List<PresupuestoHistorial> Historial { get; set; } = new();
        public ICollection<PresupuestoVersion> Versiones { get; set; } = new List<PresupuestoVersion>();

        public string? MotivoRechazo { get; set; }

        public decimal CalcularTotal()
        {
            return Items.Sum(i => i.Subtotal);
        }
    }


    public class PresupuestoItem
    {
        public int Id { get; set; }

        public string Descripcion { get; set; }

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public int? RepuestoId { get; set; }
        public Repuesto? Repuesto { get; set; }

        public int PresupuestoId { get; set; }
        public Presupuesto Presupuesto { get; set; }

        public decimal Subtotal => Cantidad * PrecioUnitario;
    }

    public class PresupuestoHistorial
    {
        public int Id { get; set; }

        public int PresupuestoId { get; set; }
        public Presupuesto Presupuesto { get; set; }

        public decimal TotalAnterior { get; set; }

        public DateTime Fecha { get; set; }

        public string Motivo { get; set; }

        public int MecanicoId { get; set; }
        public Persona Mecanico { get; set; }
    }
}
