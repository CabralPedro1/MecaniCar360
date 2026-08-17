using MecaniCar360.Models;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Patterns.Strategy
{
    public class PagoEfectivoStrategy : IPagoStrategy
    {
        public Task Procesar(Pago pago)
        {
            pago.Estado = EstadoPago.Pagado;
            return Task.CompletedTask;
        }
    }
}