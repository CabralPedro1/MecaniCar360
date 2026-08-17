namespace MecaniCar360.Patterns.Memento
{
    public class PresupuestoMemento
    {
        public decimal Total { get; }
        public string Estado { get; }
        public DateTime Fecha { get; }

        public PresupuestoMemento(
            decimal total,
            string estado,
            DateTime fecha)
        {
            Total = total;
            Estado = estado;
            Fecha = fecha;
        }
    }
}