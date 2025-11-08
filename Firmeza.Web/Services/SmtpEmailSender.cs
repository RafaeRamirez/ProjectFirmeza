using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;

namespace Firmeza.Web.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public SmtpEmailSender(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendAsync(string to, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.User))
                throw new InvalidOperationException("La configuración de correo no está completa.");

            using var message = new MailMessage
            {
                From = new MailAddress(string.IsNullOrWhiteSpace(_settings.From) ? _settings.User : _settings.From, _settings.DisplayName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(to);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.User, _settings.Password)
            };

            await client.SendMailAsync(message);
        }
    }
}
