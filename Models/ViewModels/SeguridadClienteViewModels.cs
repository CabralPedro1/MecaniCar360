using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models.ViewModels;

public sealed class VerificarCorreoClienteViewModel
{
    [Required, StringLength(4096)] public string Codigo { get; set; } = "";
}

public sealed class RestablecerPasswordClienteViewModel
{
    [Required, StringLength(43, MinimumLength = 43)] public string Token { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmarPassword { get; set; } = "";
}

public sealed class RecuperarPasswordClienteViewModel
{
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
}
