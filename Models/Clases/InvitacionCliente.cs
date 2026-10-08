using System.ComponentModel.DataAnnotations;
namespace MecaniCar360.Models;

public sealed class InvitacionCliente
{
    public int Id { get; set; }
    public int PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;
    [Required, MaxLength(100)] public string EmailDestino { get; set; } = "";
    [Required, MaxLength(64)] public string TokenHash { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaConsumida { get; set; }
    public DateTime? FechaInvalidacion { get; set; }
    public int? EmitidaPorUsuarioId { get; set; }
    public Usuario? EmitidaPorUsuario { get; set; }
}
