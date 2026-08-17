using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; }

        [Required, MaxLength(100)]
        public string EmailLogin { get; set; }

        [Required, MaxLength(200)]
        public string PasswordHash { get; set; }

        public bool Activo { get; set; } = true;
        public bool PrimerLogin { get; set; } = true;

        public string? TokenRecuperacionPassword { get; set; }
        public DateTime? TokenRecuperacionExpira { get; set; }

        public int PersonaId { get; set; }

        public Persona Persona { get; set; } = null!;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public string NombreCompleto
        {
            get
            {
                return Persona == null
                    ? Username
                    : $"{Persona.Nombre} {Persona.Apellido}";
            }
        }
    }
}