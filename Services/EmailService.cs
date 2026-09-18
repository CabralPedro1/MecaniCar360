using System.Net;
using System.Net.Mail;

namespace MecaniCar360.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // =====================================
        // ENVIAR CORREO
        // =====================================

        internal async Task EnviarCorreoAsync(
            string destino,
            string asunto,
            string cuerpo,
            bool esHtml = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(destino);
            ArgumentException.ThrowIfNullOrWhiteSpace(asunto);
            ArgumentException.ThrowIfNullOrWhiteSpace(cuerpo);

            var host = _config["Smtp:Host"];
            var user = _config["Smtp:User"];
            var password = _config["Smtp:Password"];
            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("Falta la configuración Smtp:Host.");
            if (!int.TryParse(_config["Smtp:Port"], out var port) || port < 1 || port > 65535)
                throw new InvalidOperationException("La configuración Smtp:Port falta o no es un puerto válido.");
            if (string.IsNullOrWhiteSpace(user))
                throw new InvalidOperationException("Falta la configuración Smtp:User.");
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Falta la configuración Smtp:Password.");
            if (!MailAddress.TryCreate(user, "MecaniCar360", out var remitente))
                throw new InvalidOperationException("La configuración Smtp:User no es un remitente válido.");

            var enableSsl = true;
            if (_config["Smtp:EnableSsl"] is string ssl && !bool.TryParse(ssl, out enableSsl))
                throw new InvalidOperationException("La configuración Smtp:EnableSsl no es válida.");

            using var smtp = new SmtpClient(host, port);

            smtp.Credentials = new NetworkCredential(
                user, password);

            smtp.EnableSsl = enableSsl;

            using var mail = new MailMessage
            {
                From = remitente,

                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = esHtml
            };

            mail.To.Add(destino);

            await smtp.SendMailAsync(mail);
        }
    }
}
