using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;

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

        public Task SendAsync(string to, string subject, string htmlMessage, IEnumerable<EmailAttachment>? attachments = null)
        {
            var attachmentNames = attachments?
                .Where(a => a != null && !string.IsNullOrWhiteSpace(a.FileName))
                .Select(a => a!.FileName)
                .ToList();

            _logger.LogInformation("Simulación de correo (sin SMTP). Destinatario: {To}, Asunto: {Subject}, Contenido: {Content}, Adjuntos: {Attachments}",
                to, subject, htmlMessage, attachmentNames == null || attachmentNames.Count == 0 ? "N/A" : string.Join(", ", attachmentNames));
            return Task.CompletedTask;
        }
    }
}
