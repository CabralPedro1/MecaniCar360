using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Patente
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        // =====================================
        // FAMILIAS
        // =====================================

        public List<FamiliaPatente> Familias { get; set; }
            = new();
    }
}