using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models.ViewModels;

public sealed class RegistroClienteViewModel
{
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [Required, StringLength(100)] public string Apellido { get; set; } = "";
    [Required, StringLength(100), EmailAddress] public string Email { get; set; } = "";
}
