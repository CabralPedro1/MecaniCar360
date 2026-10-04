using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models.ViewModels;

public sealed class NuevoClienteViewModel
{
    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string Apellido { get; set; } = string.Empty;
    [Required, StringLength(15), Display(Name = "DNI")]
    public string Dni { get; set; } = string.Empty;
    [StringLength(50), Display(Name = "Teléfono")]
    public string? Telefono { get; set; }
    [Required(ErrorMessage = "El email es obligatorio."), StringLength(100), EmailAddress]
    public string? Email { get; set; }
}
