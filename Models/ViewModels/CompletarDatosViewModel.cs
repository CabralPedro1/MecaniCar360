using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MecaniCar360.Models.ViewModels
{
    public class CompletarDatosViewModel
    {
        // ===== Datos NO editables =====
        [BindNever]
        [ValidateNever]
        [Display(Name = "Usuario")]
        public string Username { get; set; } = string.Empty;

        [BindNever]
        [ValidateNever]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // ===== Datos editables =====
        [RegularExpression(@"^\d{6,15}$",
            ErrorMessage = "El teléfono solo puede contener números")]
        public string? Telefono { get; set; }

        // ===== Password =====
        [Required]
        [RegularExpression(
    @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
    ErrorMessage = "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un símbolo"
)]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPassword { get; set; } = string.Empty;

    }
}
