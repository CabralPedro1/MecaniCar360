namespace MecaniCar360.Patterns.Memento
{
    public class DiagnosticoCaretaker
    {
        private readonly List<DiagnosticoMemento>
            _mementos = new();

        public void Guardar(
            DiagnosticoMemento memento)
        {
            _mementos.Add(memento);
        }

        public DiagnosticoMemento? ObtenerAnterior()
        {
            if (_mementos.Count == 0)
                return null;

            return _mementos[^1];
        }

        public IReadOnlyList<DiagnosticoMemento>
            ObtenerTodos()
        {
            return _mementos.AsReadOnly();
        }
    }
}