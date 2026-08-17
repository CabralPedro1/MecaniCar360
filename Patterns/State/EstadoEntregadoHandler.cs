using MecaniCar360.Models;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Patterns.State
{
    public class EstadoEntregadoHandler
        : IEstadoOrdenHandler
    {
        public void CambiarEstado(OrdenTrabajo orden)
        {
            orden.EstadoActual =
                EstadoOrden.Entregado;

            orden.HistorialEstados.Add(
                new OrdenTrabajoEstadoHistorial
                {
                    OrdenTrabajoId =
                        orden.Id,

                    Estado =
                        EstadoOrden.Entregado,

                    Fecha =
                        DateTime.Now
                });
        }
    }
}