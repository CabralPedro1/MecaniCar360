using MecaniCar360.Models.Enums;

namespace MecaniCar360.Models.ViewModels;

public sealed class AdministracionCuentasViewModel
{
    public List<PersonaCuentaViewModel> Personas { get; init; } = new();
}

public sealed class PersonaCuentaViewModel
{
    public int PersonaId { get; init; }
    public string? Nombre { get; init; }
    public string? Apellido { get; init; }
    public string? Dni { get; init; }
    public string? Telefono { get; init; }
    public string? Email { get; init; }
    public bool PersonaActiva { get; init; }
    public int? UsuarioId { get; init; }
    public string? Username { get; init; }
    public string? EmailLogin { get; init; }
    public bool? UsuarioActivo { get; init; }
    public ProveedorAutenticacion? Proveedor { get; init; }
    public bool EsPersonal { get; init; }
    public bool EsCliente { get; init; }
    public List<RolCuentaViewModel> Roles { get; init; } = new();
}

public sealed class RolCuentaViewModel
{
    public string Nombre { get; init; } = "";
    public bool Activo { get; init; }
    public DateTime? FechaBaja { get; init; }
}
