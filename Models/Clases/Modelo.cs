using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Modelo
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // FK
        public int MarcaId { get; set; }

        // Navigation
        public Marca Marca { get; set; } = null!;

        public List<Vehiculo> Vehiculos { get; set; } = new();
    }
}