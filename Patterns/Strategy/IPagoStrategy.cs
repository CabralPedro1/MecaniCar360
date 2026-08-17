using MecaniCar360.Models;

namespace MecaniCar360.Patterns.Strategy
{
    public interface IPagoStrategy
    {
        Task Procesar(Pago pago);
    }
}