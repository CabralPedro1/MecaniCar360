using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Marca
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Navigation
        public List<Modelo> Modelos { get; set; } = new();
    }
}