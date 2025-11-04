namespace Firmeza.WebApplication.Interfaces;

public interface IAiChatService
{
    Task<string> GetAnswerAsync(string prompt, System.Threading.CancellationToken cancellationToken = default);
}
