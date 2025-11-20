using System.Collections.Generic;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;

namespace Firmeza.Web.Services
{
    public class MailKitEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public MailKitEmailSender(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendAsync(string to, string subject, string htmlMessage, IEnumerable<EmailAttachment>? attachments = null)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host))
                throw new InvalidOperationException("El Host SMTP no está configurado.");

            if (_settings.RequireAuthentication && (string.IsNullOrWhiteSpace(_settings.User) || string.IsNullOrWhiteSpace(_settings.Password)))
                throw new InvalidOperationException("Credenciales SMTP incompletas.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                string.IsNullOrWhiteSpace(_settings.DisplayName) ? _settings.User : _settings.DisplayName,
                string.IsNullOrWhiteSpace(_settings.From) ? _settings.User : _settings.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            var builder = new BodyBuilder { HtmlBody = htmlMessage };
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    if (attachment?.Content == null || attachment.Content.Length == 0 || string.IsNullOrWhiteSpace(attachment.FileName))
                        continue;
                    builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType ?? "application/octet-stream"));
                }
            }
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            SecureSocketOptions security = _settings.Security?.ToLowerInvariant() switch
            {
                "ssl" => SecureSocketOptions.SslOnConnect,
                "starttls" => SecureSocketOptions.StartTls,
                "none" => SecureSocketOptions.None,
                _ => SecureSocketOptions.Auto
            };

            if (security == SecureSocketOptions.Auto && _settings.EnableSsl)
            {
                security = _settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            }

            await client.ConnectAsync(_settings.Host, _settings.Port, security);
            if (_settings.RequireAuthentication)
            {
                await client.AuthenticateAsync(_settings.User, _settings.Password);
            }
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
