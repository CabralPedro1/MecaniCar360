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

        public async Task EnviarCorreoAsync(
            string destino,
            string asunto,
            string cuerpo,
            bool esHtml = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(destino);
            ArgumentException.ThrowIfNullOrWhiteSpace(asunto);
            ArgumentException.ThrowIfNullOrWhiteSpace(cuerpo);

            using var smtp = new SmtpClient(
                _config["Smtp:Host"],
                int.Parse(_config["Smtp:Port"]!));

            smtp.Credentials = new NetworkCredential(
                _config["Smtp:User"],
                _config["Smtp:Password"]);

            smtp.EnableSsl = true;

            using var mail = new MailMessage
            {
                From = new MailAddress(
                    _config["Smtp:User"]!,
                    "MecaniCar360"),

                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = esHtml
            };

            mail.To.Add(destino);

            await smtp.SendMailAsync(mail);
        }
    }
}