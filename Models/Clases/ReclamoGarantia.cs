using MecaniCar360.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class ReclamoGarantia
    {
        public int Id { get; set; }


        // =====================================
        // GARANTÍA
        // =====================================

        public int GarantiaId { get; set; }

        public Garantia Garantia { get; set; }
            = null!;


        // =====================================
        // RECLAMO
        // =====================================

        public DateTime Fecha { get; set; }
            = DateTime.Now;

        [Required]
        [MaxLength(1000)]
        public string Motivo { get; set; }
            = string.Empty;

        [MaxLength(2000)]
        public string? Observaciones { get; set; }

        public EstadoReclamoGarantia Estado { get; set; }
            = EstadoReclamoGarantia.Pendiente;


        // =====================================
        // NUEVA ORDEN
        // =====================================

        public int? OrdenTrabajoNuevaId { get; set; }

        public OrdenTrabajo? OrdenTrabajoNueva { get; set; }


        // =====================================
        // AUDITORÍA
        // =====================================

        public int RegistradoPorUsuarioId { get; set; }

        public Usuario RegistradoPorUsuario { get; set; }
            = null!;
    }
}