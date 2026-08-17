using MecaniCar360.Models;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Patterns.State
{
    public class EstadoFinalizadoHandler
        : IEstadoOrdenHandler
    {
        public void CambiarEstado(OrdenTrabajo orden)
        {
            orden.EstadoActual =
                EstadoOrden.Finalizado;

            orden.HistorialEstados.Add(
                new OrdenTrabajoEstadoHistorial
                {
                    OrdenTrabajoId =
                        orden.Id,

                    Estado =
                        EstadoOrden.Finalizado,

                    Fecha =
                        DateTime.Now
                });
        }
    }
}