using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models.ViewModels
{
    public class CambiarContraseñaViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string ContraseñaActual { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string NuevaContraseña { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NuevaContraseña), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nueva contraseña")]
        public string ConfirmarNuevaContraseña { get; set; } = string.Empty;
    }
}
