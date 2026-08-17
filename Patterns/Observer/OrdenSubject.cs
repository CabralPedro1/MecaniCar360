using MecaniCar360.Models;

namespace MecaniCar360.Patterns.Observer
{
    public class OrdenSubject
    {
        private readonly List<IOrdenObserver> _observadores;

        public OrdenSubject(
            IEnumerable<IOrdenObserver> observadores)
        {
            _observadores =
                observadores.ToList();
        }

        public void Suscribir(
            IOrdenObserver observer)
        {
            if (!_observadores.Contains(observer))
            {
                _observadores.Add(observer);
            }
        }

        public void Desuscribir(
            IOrdenObserver observer)
        {
            _observadores.Remove(observer);
        }

        public async Task NotificarAsync(
            OrdenTrabajo orden,
            string evento)
        {
            foreach (var observer in _observadores)
            {
                await observer.ActualizarAsync(
                    orden,
                    evento);
            }
        }
    }
}