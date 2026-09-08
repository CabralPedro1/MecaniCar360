using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models.ViewModels
{
    public class UsuarioCreateViewModel
    {
        [Required]
        [Display(Name = "Persona")]
        public int PersonaId { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Usuario")]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        [Display(Name = "Email de acceso")]
        public string EmailLogin { get; set; } = string.Empty;
    }
}
