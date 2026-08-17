using MecaniCar360.Models;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Patterns.Strategy
{
    public class PagoTarjetaStrategy : IPagoStrategy
    {
        public Task Procesar(Pago pago)
        {
            // simulamos aprobación
            pago.Estado = EstadoPago.Pagado;
            return Task.CompletedTask;
        }
    }
}