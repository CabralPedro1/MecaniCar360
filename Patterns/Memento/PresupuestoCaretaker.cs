namespace MecaniCar360.Patterns.Memento
{
    public class PresupuestoCaretaker
    {
        private readonly List<PresupuestoMemento>
            _mementos = new();

        public void Guardar(
            PresupuestoMemento memento)
        {
            _mementos.Add(memento);
        }

        public PresupuestoMemento? ObtenerAnterior()
        {
            if (_mementos.Count == 0)
                return null;

            return _mementos[^1];
        }

        public IReadOnlyList<PresupuestoMemento>
            ObtenerTodos()
        {
            return _mementos.AsReadOnly();
        }
    }
}