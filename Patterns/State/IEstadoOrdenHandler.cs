using MecaniCar360.Models;

namespace MecaniCar360.Patterns.State
{
    public interface IEstadoOrdenHandler
    {
        void CambiarEstado(OrdenTrabajo orden);
    }
}