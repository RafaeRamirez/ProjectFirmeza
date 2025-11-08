using System.Threading.Tasks;

namespace Firmeza.Web.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string htmlMessage);
    }
}
