namespace MecaniCar360.Models.ViewModels;

public class ClienteOperativoViewModel
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Dni { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; }
}

public class ClientesOperativosViewModel
{
    public string? Busqueda { get; set; }
    public List<ClienteOperativoViewModel> Clientes { get; set; } = new();
}

public class ClienteDetalleOperativoViewModel
{
    public ClienteOperativoViewModel Cliente { get; set; } = new();
    public bool PuedeVerVehiculos { get; set; }
    public List<VehiculoClienteOperativoViewModel> Vehiculos { get; set; } = new();
}

public class VehiculoClienteOperativoViewModel
{
    public string Patente { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }
    public string? Color { get; set; }
    public bool Activo { get; set; }
}
