namespace MecaniCar360.Patterns.Memento
{
    public class DiagnosticoOriginator
    {
        public string Descripcion { get; private set; }

        public DiagnosticoOriginator(
            string descripcion)
        {
            Descripcion = descripcion;
        }

        public void Actualizar(
            string nuevaDescripcion)
        {
            Descripcion = nuevaDescripcion;
        }

        public DiagnosticoMemento CrearMemento()
        {
            return new DiagnosticoMemento(
                Descripcion,
                DateTime.Now);
        }

        public void Restaurar(
            DiagnosticoMemento memento)
        {
            Descripcion = memento.Descripcion;
        }
    }
}