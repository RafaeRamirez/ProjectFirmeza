using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firmeza.Web.Data;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Firmeza.Web.Services
{
    public class DbChatBotSettingsProvider : IChatBotSettingsProvider
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DbChatBotSettingsProvider> _logger;

        public DbChatBotSettingsProvider(AppDbContext db, ILogger<DbChatBotSettingsProvider> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ChatBotSettings?> GetAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.ChatBotSettings
                    .AsNoTracking()
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo cargar la configuración del chatbot desde la base de datos.");
                return null;
            }
        }
    }
}
