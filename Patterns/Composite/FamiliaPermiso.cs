using MecaniCar360.Models;

namespace MecaniCar360.Patterns.Composite
{
    public class FamiliaPermiso : IComponentePermiso
    {
        private readonly Familia _familia;

        private readonly List<IComponentePermiso>
            _componentes = new();

        public FamiliaPermiso(
            Familia familia)
        {
            _familia = familia;
        }

        public int Id =>
            _familia.Id;

        public string Nombre =>
            _familia.Nombre;


        public void Agregar(
            IComponentePermiso componente)
        {
            if (!_componentes.Contains(
                componente))
            {
                _componentes.Add(
                    componente);
            }
        }


        public bool TienePermiso(
            string patente)
        {
            if (!_familia.Activo)
            {
                return false;
            }

            return _componentes.Any(
                componente =>
                    componente.TienePermiso(
                        patente));
        }
    }
}