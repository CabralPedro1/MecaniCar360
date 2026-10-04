using System.ComponentModel.DataAnnotations;
namespace MecaniCar360.Models.ViewModels;

public sealed class ActivarClienteViewModel
{
    [Required, StringLength(43, MinimumLength = 43)] public string Token { get; set; } = "";
    [Required, MaxLength(50), RegularExpression(@"[^@]+")]
    public string Username { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmarPassword { get; set; } = "";
}

public sealed record EstadoCuentaCliente(bool TieneUsuario, bool Habilitado, bool PuedeInvitar, bool InvitacionPendiente);
