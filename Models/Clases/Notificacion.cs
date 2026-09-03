using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Notificacion
    {
        public int Id { get; set; }

        public int PersonaId { get; set; }
        public Persona Persona { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Mensaje { get; set; } = string.Empty;

        public bool Leida { get; set; } = false;

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}