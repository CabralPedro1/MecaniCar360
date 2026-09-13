using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Auditoria
    {
        public int Id { get; set; }

        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        [Required]
        [MaxLength(100)]
        public string Accion { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Entidad { get; set; } = string.Empty;

        public int? EntidadId { get; set; }

        [MaxLength(2000)]
        public string? Descripcion { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
