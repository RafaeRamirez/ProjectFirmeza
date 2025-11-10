using Firmeza.Api.Services.Abstractions;

namespace Firmeza.Api.Services.Email;

public sealed class NullEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
