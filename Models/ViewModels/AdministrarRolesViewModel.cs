using MecaniCar360.Models;

namespace MecaniCar360.ViewModels
{
    public class AdministrarRolesViewModel
    {
        public Persona Persona { get; set; }

        public List<Rol> RolesActuales { get; set; } = new();

        public List<Rol> RolesDisponibles { get; set; } = new();
    }
}