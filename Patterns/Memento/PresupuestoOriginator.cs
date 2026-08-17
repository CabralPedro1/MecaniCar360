namespace MecaniCar360.Patterns.Memento
{
    public class PresupuestoOriginator
    {
        public decimal Total { get; private set; }

        public string Estado { get; private set; }

        public PresupuestoOriginator(
            decimal total,
            string estado)
        {
            Total = total;
            Estado = estado;
        }

        public void Actualizar(
            decimal nuevoTotal,
            string nuevoEstado)
        {
            Total = nuevoTotal;
            Estado = nuevoEstado;
        }

        public PresupuestoMemento CrearMemento()
        {
            return new PresupuestoMemento(
                Total,
                Estado,
                DateTime.Now);
        }

        public void Restaurar(
            PresupuestoMemento memento)
        {
            Total = memento.Total;
            Estado = memento.Estado;
        }
    }
}