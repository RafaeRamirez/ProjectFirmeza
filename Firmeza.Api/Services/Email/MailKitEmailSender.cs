using Firmeza.Api.Domain.Settings;
using Firmeza.Api.Services.Abstractions;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Firmeza.Api.Services.Email;

public class MailKitEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;

    public MailKitEmailSender(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.From))
        {
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.DisplayName, _settings.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var client = new MailKit.Net.Smtp.SmtpClient();
        var secureSocket = _settings.Security?.ToLowerInvariant() switch
        {
            "ssl" => SecureSocketOptions.SslOnConnect,
            "starttls" => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.Auto
        };

        await client.ConnectAsync(_settings.Host, _settings.Port, secureSocket, cancellationToken);
        if (_settings.RequireAuthentication && !string.IsNullOrWhiteSpace(_settings.User))
        {
            await client.AuthenticateAsync(_settings.User, _settings.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
