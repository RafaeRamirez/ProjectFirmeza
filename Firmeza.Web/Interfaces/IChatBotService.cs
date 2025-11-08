using System.Threading.Tasks;

namespace Firmeza.Web.Interfaces
{
    public interface IChatBotService
    {
        Task<string> AskAsync(string message, string? userId);
    }
}
