using MecaniCar360.Models;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Patterns.Strategy
{
    public class PagoTransferenciaStrategy : IPagoStrategy
    {
        public Task Procesar(Pago pago)
        {
            // podría quedar pendiente hasta validar
            pago.Estado = EstadoPago.Pagado;
            return Task.CompletedTask;
        }
    }
}