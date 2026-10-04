namespace MecaniCar360.ViewModels;

public sealed class AdministrarRolesViewModel
{
    public int PersonaId { get; init; }
    public string? Nombre { get; init; }
    public string? Apellido { get; init; }
    public string? Dni { get; init; }
    public bool TieneCuenta { get; init; }
    public bool PuedeAsignar { get; set; }
    public List<RolInternoViewModel> RolesActuales { get; init; } = new();
    public List<RolInternoViewModel> RolesDisponibles { get; init; } = new();
}

public sealed class RolInternoViewModel
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public bool Activo { get; init; }
    public bool PuedeQuitar { get; set; }
    public string? Restriccion { get; set; }
}
