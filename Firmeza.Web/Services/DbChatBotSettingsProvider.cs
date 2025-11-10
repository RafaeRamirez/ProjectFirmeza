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
                var settings = await _db.ChatBotSettings
                    .AsNoTracking()
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                return MergeWithEnvironment(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo cargar la configuración del chatbot desde la base de datos.");
                return MergeWithEnvironment(null);
            }
        }

        private ChatBotSettings? MergeWithEnvironment(ChatBotSettings? current)
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                         ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
            var model = Environment.GetEnvironmentVariable("GEMINI_MODEL");
            var scope = Environment.GetEnvironmentVariable("GEMINI_OAUTH_SCOPE")
                        ?? Environment.GetEnvironmentVariable("GEMINI_SCOPE")
                        ?? "https://www.googleapis.com/auth/generative-language";
            var endpoint = Environment.GetEnvironmentVariable("GEMINI_ENDPOINT");
            var saPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

            if (current is null && string.IsNullOrWhiteSpace(apiKey) && string.IsNullOrWhiteSpace(saPath))
                return null;

            current ??= new ChatBotSettings();

            if (!string.IsNullOrWhiteSpace(apiKey))
                current.ApiKey = apiKey;
            if (!string.IsNullOrWhiteSpace(saPath))
                current.ServiceAccountJsonPath = saPath;
            if (!string.IsNullOrWhiteSpace(scope))
                current.Scope = scope;
            if (!string.IsNullOrWhiteSpace(model))
                current.Model = model;
            if (!string.IsNullOrWhiteSpace(endpoint))
                current.Endpoint = endpoint;

            return current;
        }
    }
}
