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

        [MaxLength(200)]
        public string? PasswordHash { get; set; }

        public List<IdentidadExterna> IdentidadesExternas { get; set; } = new();

        public const string SecurityStampClaim = "SecurityStamp";

        [Required, MaxLength(32)]
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

        public bool Activo { get; set; } = true;
        public bool PrimerLogin { get; set; } = true;


        public int PersonaId { get; set; }

        public Persona Persona { get; set; } = null!;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public string NombreCompleto
        {
            get
            {
                var nombre = Persona == null ? null : $"{Persona.Nombre} {Persona.Apellido}".Trim();
                return string.IsNullOrWhiteSpace(nombre) ? Username : nombre;
            }
        }
    }
}
