using MecaniCar360.Models;

namespace MecaniCar360.Models.ViewModels
{
    public class DashboardViewModel
    {
        // CLIENTE
        public List<Turno> TurnosCliente { get; set; } = new();
        public List<Presupuesto> PresupuestosPendientes { get; set; } = new();

        // MECANICO
        public List<OrdenTrabajo> OrdenesMecanico { get; set; } = new();

        // CAJA
        public List<Turno> TurnosHoy { get; set; } = new();
        public List<Factura> FacturasPendientes { get; set; } = new();

        // STOCK
        public List<Repuesto> StockBajo { get; set; } = new();

        // ADMIN
        public int TotalTurnosHoy { get; set; }
        public int OrdenesActivas { get; set; }
        public decimal IngresosHoy { get; set; }
    }
}