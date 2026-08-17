using MecaniCar360.Models;

namespace MecaniCar360.Models.ViewModels
{
    public class CalendarioViewModel
    {
        public DateTime Fecha { get; set; }

        public List<HorarioItem> Horarios { get; set; } = new();
    }

    public class HorarioItem
    {
        public DateTime Hora { get; set; }

        public int Ocupados { get; set; }

        public int CapacidadTotal { get; set; }

        public List<Turno> Turnos { get; set; } = new();

        // 🔥 porcentaje seguro (clave para UI)
        public int PorcentajeOcupacion
        {
            get
            {
                if (CapacidadTotal == 0) return 0;
                return (Ocupados * 100) / CapacidadTotal;
            }
        }

        // 🔥 estado visual (te simplifica la vista)
        public string ClaseColor
        {
            get
            {
                if (PorcentajeOcupacion > 80) return "bg-danger";
                if (PorcentajeOcupacion > 50) return "bg-warning";
                return "bg-success";
            }
        }
    }
}