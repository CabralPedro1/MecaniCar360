using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.ViewModels
{
    public class PersonaFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [Display(Name = "Nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [Display(Name = "Apellido")]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El DNI es obligatorio.")]
        [Display(Name = "DNI")]
        [StringLength(20)]
        public string Dni { get; set; } = string.Empty;

        [Display(Name = "Teléfono")]
        [StringLength(30)]
        public string? Telefono { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;
    }
}