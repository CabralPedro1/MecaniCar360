using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Persona
    {
        public int Id { get; set; }


        // =====================================
        // DATOS PERSONALES
        // =====================================

        [MaxLength(100)]
        public string? Nombre { get; set; }

        [MaxLength(100)]
        public string? Apellido { get; set; }

        [MaxLength(15)]
        public string? Dni { get; set; }


        // =====================================
        // CONTACTO
        // =====================================

        [MaxLength(50)]
        public string? Telefono { get; set; }

        [MaxLength(100)]
        [EmailAddress]
        public string? Email { get; set; }


        // =====================================
        // ESTADO
        // =====================================

        public bool Activo { get; set; } = true;


        // =====================================
        // ROLES
        // =====================================

        public List<PersonaRol> Roles { get; set; } = new();


        // =====================================
        // USUARIO
        // =====================================

        public Usuario? Usuario { get; set; }
    }
}
