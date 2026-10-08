using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models;

public enum FinalidadPasswordCliente { Agregar = 1, Cambiar = 2, Recuperar = 3 }

// Un único enlace vigente por cuenta. No acredita el alta ni la verificación histórica del correo.
public sealed class EnlacePasswordCliente
{
    [Key] public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    [MaxLength(64)] public string TokenHash { get; set; } = "";
    [MaxLength(64)] public string StampHash { get; set; } = "";
    [MaxLength(64)] public string EmailHash { get; set; } = "";
    public bool TeniaPassword { get; set; }
    public FinalidadPasswordCliente Finalidad { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaConsumida { get; set; }
    public DateTime? FechaInvalidacion { get; set; }
}
