using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Persona
    {
        public int Id { get; set; }


        // =====================================
        // DATOS PERSONALES
        // =====================================

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string Dni { get; set; } = string.Empty;


        // =====================================
        // CONTACTO
        // =====================================

        [MaxLength(50)]
        public string Telefono { get; set; } = string.Empty;

        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;


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