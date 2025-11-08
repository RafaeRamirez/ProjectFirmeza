using System.Threading;
using System.Threading.Tasks;
using Firmeza.Web.Models;

namespace Firmeza.Web.Interfaces
{
    public interface IChatBotSettingsProvider
    {
        Task<ChatBotSettings?> GetAsync(CancellationToken cancellationToken = default);
    }
}
