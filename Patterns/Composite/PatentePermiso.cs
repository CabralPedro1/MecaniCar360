using MecaniCar360.Models;

namespace MecaniCar360.Patterns.Composite
{
    public class PatentePermiso : IComponentePermiso
    {
        private readonly Patente _patente;

        public PatentePermiso(Patente patente)
        {
            _patente = patente;
        }

        public int Id =>
            _patente.Id;

        public string Nombre =>
            _patente.Nombre;

        public bool TienePermiso(
            string patente)
        {
            return _patente.Activo &&
                   string.Equals(
                       _patente.Nombre,
                       patente,
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}