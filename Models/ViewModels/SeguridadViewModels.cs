using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models.ViewModels;

public sealed class FamiliaEdicionViewModel
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [StringLength(250)] public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

public sealed class SeguridadViewModel
{
    public List<Familia> Familias { get; set; } = new();
    public List<Patente> Patentes { get; set; } = new();
    public List<Rol> Roles { get; set; } = new();
}
