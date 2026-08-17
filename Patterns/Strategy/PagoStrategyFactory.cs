using MecaniCar360.Models.Enums;

namespace MecaniCar360.Patterns.Strategy
{
    public static class PagoStrategyFactory
    {
        public static IPagoStrategy Crear(MetodoPago metodo)
        {
            return metodo switch
            {
                MetodoPago.Efectivo => new PagoEfectivoStrategy(),
                MetodoPago.TarjetaCredito => new PagoTarjetaStrategy(),
                MetodoPago.TarjetaDebito => new PagoTarjetaStrategy(),
                MetodoPago.Transferencia => new PagoTransferenciaStrategy(),

                _ => throw new NotImplementedException("Método de pago no soportado")
            };
        }
    }
}