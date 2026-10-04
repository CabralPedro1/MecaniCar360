using System.ComponentModel.DataAnnotations;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Models;

public sealed class IdentidadExterna
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public ProveedorIdentidadExterna Proveedor { get; set; }
    [Required, MaxLength(255)]
    public string IdentificadorExterno { get; set; } = string.Empty;
    public DateTime FechaVinculacion { get; set; } = DateTime.UtcNow;
}
