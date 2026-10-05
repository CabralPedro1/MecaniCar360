namespace MecaniCar360.Models.ViewModels;

public sealed class InicioOperativoViewModel
{
    public string Titulo { get; set; } = "";
    public List<AccesoOperativo> Accesos { get; set; } = new();
    public List<Turno> Turnos { get; set; } = new();
    public List<OrdenTrabajo> Ordenes { get; set; } = new();
    public List<ExistenciaOperativa> Existencias { get; set; } = new();
    public bool VerTurnos { get; set; }
    public bool VerOrdenes { get; set; }
    public bool VerDetalleOrden { get; set; }
    public bool VerExistencias { get; set; }
}
public sealed record AccesoOperativo(string Texto, string Controller, string Action);
public sealed record ExistenciaOperativa(int Id, string Nombre, int Actual, int Minimo);
