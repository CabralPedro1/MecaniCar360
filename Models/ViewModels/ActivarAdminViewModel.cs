using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models.ViewModels;

public sealed class ActivarAdminViewModel
{
    [Required, StringLength(43, MinimumLength = 43)] public string Token { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmarPassword { get; set; } = "";
    [RegularExpression(@"^\d{6,15}$", ErrorMessage = "El teléfono solo puede contener entre 6 y 15 números.")]
    public string? Telefono { get; set; }
}
