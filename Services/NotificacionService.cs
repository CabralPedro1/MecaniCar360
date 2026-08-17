using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;

namespace MecaniCar360.Services
{
    public class NotificacionService
    {
        private readonly MecaniCarContext _context;

        public NotificacionService(MecaniCarContext context)
        {
            _context = context;
        }

        // =====================================
        // CREAR NOTIFICACIÓN
        // =====================================

        public async Task<ServiceResult> NotificarAsync(
            int personaId,
            string mensaje)
        {
            if (string.IsNullOrWhiteSpace(mensaje))
                return ServiceResult.Error(
                    "Debe ingresar un mensaje.");

            var notificacion = new Notificacion
            {
                PersonaId = personaId,
                Mensaje = mensaje,
                Fecha = DateTime.Now,
                Leida = false
            };

            _context.Notificaciones.Add(notificacion);

            await GuardarCambiosAsync();

            return ServiceResult.Ok(
                "Notificación creada correctamente.");
        }

        // =====================================
        // OPERACIONES
        // =====================================

        private async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}