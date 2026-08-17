namespace MecaniCar360.Patterns.Memento
{
    public class DiagnosticoMemento
    {
        public string Descripcion { get; }
        public DateTime Fecha { get; }

        public DiagnosticoMemento(
            string descripcion,
            DateTime fecha)
        {
            Descripcion = descripcion;
            Fecha = fecha;
        }
    }
}