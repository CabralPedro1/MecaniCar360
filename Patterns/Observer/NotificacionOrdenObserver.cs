using MecaniCar360.Models;
using MecaniCar360.Services;

namespace MecaniCar360.Patterns.Observer
{
    public class NotificacionOrdenObserver : IOrdenObserver
    {
        private readonly NotificacionService _notificacionService;

        public NotificacionOrdenObserver(
            NotificacionService notificacionService)
        {
            _notificacionService = notificacionService;
        }

        public async Task ActualizarAsync(
            OrdenTrabajo orden,
            string evento)
        {
            if (orden.Turno == null)
            {
                return;
            }

            var mensaje =
                $"Orden de trabajo #{orden.Id}: {evento}";

            await _notificacionService
                .NotificarAsync(
                    orden.Turno.ClienteId,
                    mensaje);
        }
    }
}