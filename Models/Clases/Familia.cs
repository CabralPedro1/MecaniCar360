using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Familia
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;


        // =====================================
        // FAMILIA PADRE
        // =====================================

        public int? FamiliaPadreId { get; set; }

        public Familia? FamiliaPadre { get; set; }


        // =====================================
        // FAMILIAS HIJAS
        // =====================================

        public List<Familia> FamiliasHijas { get; set; }
            = new();


        // =====================================
        // PATENTES
        // =====================================

        public List<FamiliaPatente> Patentes { get; set; }
            = new();


        // =====================================
        // ROLES
        // =====================================

        public List<RolFamilia> Roles { get; set; }
            = new();
    }
}