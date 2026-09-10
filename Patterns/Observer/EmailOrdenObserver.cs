using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Services;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Patterns.Observer
{
    public class EmailOrdenObserver : IOrdenObserver
    {
        private readonly MecaniCarContext _context;
        private readonly EmailService _emailService;

        public EmailOrdenObserver(
            MecaniCarContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task ActualizarAsync(
            OrdenTrabajo orden,
            string evento)
        {
            if (orden.IngresoVehiculo?.Turno == null)
            {
                return;
            }

            var cliente = await _context.Personas
                .FirstOrDefaultAsync(p =>
                    p.Id == orden.IngresoVehiculo.Turno.ClienteId);

            if (cliente == null ||
                string.IsNullOrWhiteSpace(cliente.Email))
            {
                return;
            }

            string asunto =
                $"MecaniCar360 - Actualización de orden #{orden.Id}";

            string cuerpo = $@"
                <h2>MecaniCar360</h2>

                <p>Hola {cliente.Nombre} {cliente.Apellido},</p>

                <p>
                    Se produjo una actualización en su orden de trabajo
                    <strong>#{orden.Id}</strong>.
                </p>

                <p>
                    <strong>Evento:</strong> {evento}
                </p>

                <p>
                    Estado actual:
                    <strong>{orden.EstadoActual}</strong>
                </p>

                <p>
                    Saludos,<br/>
                    MecaniCar360
                </p>";

            try
            {
                await _emailService.EnviarCorreoAsync(
                    cliente.Email,
                    asunto,
                    cuerpo,
                    true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"No se pudo enviar el correo de la orden {orden.Id}: {ex.Message}");
            }
        }
    }
}