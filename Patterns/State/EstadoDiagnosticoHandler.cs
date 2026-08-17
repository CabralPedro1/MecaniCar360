using MecaniCar360.Models;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Patterns.State
{
    public class EstadoDiagnosticoHandler : IEstadoOrdenHandler
    {
        public void CambiarEstado(OrdenTrabajo orden)
        {
            orden.EstadoActual =
                EstadoOrden.Diagnostico;

            orden.HistorialEstados.Add(
                new OrdenTrabajoEstadoHistorial
                {
                    OrdenTrabajoId =
                        orden.Id,

                    Estado =
                        EstadoOrden.Diagnostico,

                    Fecha =
                        DateTime.Now
                });
        }
    }
}