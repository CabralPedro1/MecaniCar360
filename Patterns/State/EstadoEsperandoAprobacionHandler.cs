using MecaniCar360.Models;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Patterns.State
{
    public class EstadoEsperandoAprobacionHandler
        : IEstadoOrdenHandler
    {
        public void CambiarEstado(OrdenTrabajo orden)
        {
            orden.EstadoActual =
                EstadoOrden.EsperandoAprobacion;

            orden.HistorialEstados.Add(
                new OrdenTrabajoEstadoHistorial
                {
                    OrdenTrabajoId =
                        orden.Id,

                    Estado =
                        EstadoOrden.EsperandoAprobacion,

                    Fecha =
                        DateTime.Now
                });
        }
    }
}