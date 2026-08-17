using MecaniCar360.Models;

namespace MecaniCar360.Patterns.Observer
{
    public interface IOrdenObserver
    {
        Task ActualizarAsync(
            OrdenTrabajo orden,
            string evento);
    }
}