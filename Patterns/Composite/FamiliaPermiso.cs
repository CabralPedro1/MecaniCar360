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

        internal bool Activo => _familia.Activo;

        internal IReadOnlyList<IComponentePermiso> Componentes => _componentes.AsReadOnly();


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
            return ObtenerPatentes().Contains(patente, StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> ObtenerPatentes() =>
            RecorridoPermisos.ObtenerPatentes(new[] { this });
    }
}
