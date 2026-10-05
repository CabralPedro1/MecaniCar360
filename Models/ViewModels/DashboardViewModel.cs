namespace MecaniCar360.Models.ViewModels;

public sealed class DashboardViewModel
{
    public int TurnosHoy { get; set; }
    public int EnTaller { get; set; }
    public int OrdenesActivas { get; set; }
    public int EsperandoAprobacion { get; set; }
    public int EnReparacion { get; set; }
    public int PendientesEntrega { get; set; }
    public decimal FacturadoMes { get; set; }
    public decimal SaldoPendiente { get; set; }
    public List<DashboardMes> Meses { get; set; } = new();
    public List<AuditoriaConsultaViewModel> Actividad { get; set; } = new();
    public List<DashboardAcceso> Accesos { get; set; } = new();
}
public sealed record DashboardMes(DateTime Mes, int Cantidad);
public sealed record DashboardAcceso(string Nombre, string Controller, string Action);
