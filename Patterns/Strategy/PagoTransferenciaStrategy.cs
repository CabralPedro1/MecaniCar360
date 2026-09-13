using MecaniCar360.Models;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Patterns.Strategy
{
    public class PagoTransferenciaStrategy : IPagoStrategy
    {
        public Task Procesar(Pago pago)
        {
            // Simulación académica: no verifica una transferencia bancaria real.
            pago.Estado = EstadoPago.Pagado;
            return Task.CompletedTask;
        }
    }
}
