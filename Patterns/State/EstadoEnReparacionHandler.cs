using MecaniCar360.Models;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Patterns.State
{
    public class EstadoEnReparacionHandler
        : IEstadoOrdenHandler
    {
        public void CambiarEstado(OrdenTrabajo orden)
        {
            orden.EstadoActual =
                EstadoOrden.EnReparacion;

            orden.HistorialEstados.Add(
                new OrdenTrabajoEstadoHistorial
                {
                    OrdenTrabajoId =
                        orden.Id,

                    Estado =
                        EstadoOrden.EnReparacion,

                    Fecha =
                        DateTime.Now
                });
        }
    }
}