using MecaniCar360.Models;

namespace MecaniCar360.Models.ViewModels
{
    public class AgendaViewModel
    {
        // =====================================
        // FECHA MOSTRADA
        // =====================================

        public DateTime Fecha { get; set; }


        // =====================================
        // TURNOS DEL DÍA
        // =====================================

        public List<Turno> Turnos { get; set; } = new();


        // =====================================
        // HORARIOS DISPONIBLES
        // =====================================

        public List<DateTime> HorariosDisponibles { get; set; } = new();
    }
}