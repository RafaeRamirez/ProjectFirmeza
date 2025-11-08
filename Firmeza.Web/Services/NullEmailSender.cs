using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Firmeza.Web.Interfaces;

namespace Firmeza.Web.Services
{
    /// <summary>
    /// Emisor "dummy" para entornos de desarrollo sin SMTP configurado.
    /// Simplemente registra el correo en el log y retorna con éxito.
    /// </summary>
    public class NullEmailSender : IEmailSender
    {
        private readonly ILogger<NullEmailSender> _logger;

        public NullEmailSender(ILogger<NullEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string to, string subject, string htmlMessage)
        {
            _logger.LogInformation("Simulación de correo (sin SMTP). Destinatario: {To}, Asunto: {Subject}, Contenido: {Content}",
                to, subject, htmlMessage);
            return Task.CompletedTask;
        }
    }
}
