using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Vehiculo
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Patente { get; set; } = string.Empty;

        [StringLength(17)]
        public string? Vin { get; set; }

        // Marca
        public int MarcaId { get; set; }
        public Marca Marca { get; set; } = null!;

        // Modelo
        public int ModeloId { get; set; }
        public Modelo Modelo { get; set; } = null!;

        [Range(1900, 2100)]
        public int Anio { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }

        [StringLength(50)]
        public string? Motor { get; set; }

        [StringLength(50)]
        public string? Version { get; set; }

        public int? Kilometraje { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public bool Activo { get; set; } = true;

        // Navegación
        public List<DominioVehicular> DominiosVehiculares { get; set; } = new();

        public List<Turno> Turnos { get; set; } = new();

        public List<OrdenTrabajo> OrdenesTrabajo { get; set; } = new();
    }
}