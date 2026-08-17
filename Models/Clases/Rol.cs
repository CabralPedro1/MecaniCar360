using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Rol
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        // Indica si es un rol destinado a clientes.
        public bool EsRolCliente { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public bool Activo { get; set; } = true;

        // Personas que poseen este rol.
        public List<PersonaRol> Personas { get; set; } = new();
    }


    public class PersonaRol
    {
        public int PersonaId { get; set; }

        public Persona Persona { get; set; } = null!;


        public int RolId { get; set; }

        public Rol Rol { get; set; } = null!;


        // =====================================
        // VIGENCIA DEL ROL
        // =====================================

        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public DateTime? FechaBaja { get; set; }


        // =====================================
        // AUDITORÍA
        // =====================================

        public int? OtorgadoPorUsuarioId { get; set; }

        public Usuario? OtorgadoPorUsuario { get; set; }
    }
}