using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models.ViewModels
{
    public class CompletarDatosViewModel
    {
        // ===== Datos NO editables =====
        public int UsuarioId { get; set; }

        [Display(Name = "Usuario")]
        public string Username { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        // ===== Datos editables =====
        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$",
            ErrorMessage = "El nombre solo puede contener letras")]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$",
            ErrorMessage = "El apellido solo puede contener letras")]
        public string Apellido { get; set; }

        [Required]
        [StringLength(8, MinimumLength = 7)]
        [RegularExpression(@"^\d+$", ErrorMessage = "El DNI solo puede contener números")]
        public string Dni { get; set; }

        [RegularExpression(@"^\d{6,15}$",
            ErrorMessage = "El teléfono solo puede contener números")]
        public string Telefono { get; set; }

        // ===== Password =====
        [Required]
        [RegularExpression(
    @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
    ErrorMessage = "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un símbolo"
)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPassword { get; set; }

    }
}
