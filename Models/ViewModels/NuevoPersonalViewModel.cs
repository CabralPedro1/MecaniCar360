using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models.ViewModels;

public sealed class NuevoPersonalViewModel
{
    [Required, StringLength(100)]
    public string Nombre { get; set; } = "";
    [Required, StringLength(100)]
    public string Apellido { get; set; } = "";
    [Required, StringLength(15), Display(Name = "DNI")]
    public string Dni { get; set; } = "";
    [StringLength(30), Display(Name = "Teléfono")]
    public string? Telefono { get; set; }
    [EmailAddress, StringLength(100), Display(Name = "Email de contacto")]
    public string? Email { get; set; }
    [Required, StringLength(50), Display(Name = "Usuario")]
    [RegularExpression(@"[^@]+", ErrorMessage = "El usuario no puede contener @.")]
    public string Username { get; set; } = "";
    [Required, EmailAddress, StringLength(100), Display(Name = "Email de acceso")]
    public string EmailLogin { get; set; } = "";
    [Range(1, int.MaxValue), Display(Name = "Rol interno inicial")]
    public int RolId { get; set; }
}
