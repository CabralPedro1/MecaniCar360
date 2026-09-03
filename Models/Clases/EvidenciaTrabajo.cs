using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class EvidenciaTrabajo
    {
        public int Id { get; set; }

        public int OrdenTrabajoId { get; set; }
        public OrdenTrabajo OrdenTrabajo { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string RutaArchivo { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Now;

        public int SubidaPorUsuarioId { get; set; }
        public Usuario SubidaPorUsuario { get; set; } = null!;
    }
}