using MecaniCar360.Models;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Patterns.State
{
    public class OrdenStateService
    {
        public void CambiarEstado(
            OrdenTrabajo orden,
            IEstadoOrdenHandler handler)
        {
            if (orden == null)
                throw new ArgumentNullException(nameof(orden));

            if (orden.EstadoActual == EstadoOrden.Entregado)
            {
                throw new InvalidOperationException(
                    "La orden ya fue entregada y no puede cambiar de estado.");
            }

            handler.CambiarEstado(orden);
        }
    }
}