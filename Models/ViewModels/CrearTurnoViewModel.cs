using MecaniCar360.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models.ViewModels
{
    public class CrearTurnoViewModel
    {
        // =====================================
        // CLIENTE
        // =====================================

        [Required]
        public int ClienteId { get; set; }


        // =====================================
        // VEHÍCULO
        // =====================================

        [Required]
        public int VehiculoId { get; set; }


        // =====================================
        // TIPO DE TURNO
        // =====================================

        [Required]
        public TipoTurno Tipo { get; set; }


        // =====================================
        // FECHA Y HORARIO
        // =====================================

        [Required]
        [Display(Name = "Fecha y hora")]
        public DateTime FechaInicio { get; set; }


        // =====================================
        // MOTIVO
        // =====================================

        [Required]
        [StringLength(500)]
        [Display(Name = "Motivo")]
        public string Motivo { get; set; } = string.Empty;


        // =====================================
        // OBSERVACIONES
        // =====================================

        [StringLength(1000)]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }


        // =====================================
        // DATOS PARA LA VISTA
        // =====================================

        public string? NombreCliente { get; set; }

        public string? VehiculoDescripcion { get; set; }
    }
}