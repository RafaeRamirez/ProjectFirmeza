using System.Collections.Generic;
using System.Threading.Tasks;
using Firmeza.Web.Models;

namespace Firmeza.Web.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string htmlMessage, IEnumerable<EmailAttachment>? attachments = null);
    }
}
