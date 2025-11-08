using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;

namespace Firmeza.Web.Services
{
    public class GoogleAiChatBotService : IChatBotService
    {
        private readonly HttpClient _http;
        private readonly GoogleApiClient _googleApiClient;
        private readonly IChatBotSettingsProvider _settingsProvider;
        private readonly ILogger<GoogleAiChatBotService> _logger;

        private class RequestModel
        {
            public Content[] contents { get; set; } = new[] { new Content() };
            public class Content
            {
                public Part[] parts { get; set; } = new[] { new Part() };
            }
            public class Part
            {
                public string text { get; set; } = string.Empty;
            }
        }

        private class ResponseModel
        {
            public Candidate[] candidates { get; set; } = System.Array.Empty<Candidate>();
            public class Candidate
            {
                public Content content { get; set; } = new Content();
            }
            public class Content
            {
                public Part[] parts { get; set; } = System.Array.Empty<Part>();
            }
            public class Part
            {
                public string text { get; set; } = string.Empty;
            }
        }

        public GoogleAiChatBotService(
            HttpClient http,
            GoogleApiClient googleApiClient,
            IChatBotSettingsProvider settingsProvider,
            ILogger<GoogleAiChatBotService> logger)
        {
            _http = http;
            _googleApiClient = googleApiClient;
            _settingsProvider = settingsProvider;
            _logger = logger;
        }

        public async Task<string> AskAsync(string message, string? userId)
        {
            var settings = await _settingsProvider.GetAsync();
            if (settings is null)
                return "El chatbot no está configurado en la base de datos.";

            if (!HasServiceAccountCredentials(settings) && string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                return "El chatbot no está configurado. Agrega tu API key o credenciales de Service Account.";
            }

            var endpoint = BuildEndpoint(settings, includeApiKey: !HasServiceAccountCredentials(settings));
            var payload = new RequestModel
            {
                contents = new[]
                {
                    new RequestModel.Content
                    {
                        parts = new[]
                        {
                            new RequestModel.Part
                            {
                                text = $"Eres un asistente de negocio para Firmeza. Usuario: {userId ?? "Invitado"}. Pregunta: {message}"
                            }
                        }
                    }
                }
            };

            try
            {
                using var response = await SendPayloadAsync(settings, endpoint, payload);
                response.EnsureSuccessStatusCode();
                var data = await response.Content.ReadFromJsonAsync<ResponseModel>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                var text = data?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text;
                return string.IsNullOrWhiteSpace(text) ? "No pude generar una respuesta en este momento." : text.Trim();
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or FileNotFoundException)
            {
                _logger.LogError(ex, "Error al consultar la API de Google AI Studio.");
                return "No pude conectar con el servicio de IA. Revisa las credenciales configuradas o tu conexión.";
            }
        }

        private static bool HasServiceAccountCredentials(ChatBotSettings settings)
        {
            return !string.IsNullOrWhiteSpace(settings.Scope)
                   && !string.IsNullOrWhiteSpace(settings.ServiceAccountJsonPath);
        }

        private static string BuildEndpoint(ChatBotSettings settings, bool includeApiKey)
        {
            var baseUrl = string.IsNullOrWhiteSpace(settings.Endpoint)
                ? "https://generativelanguage.googleapis.com"
                : settings.Endpoint.TrimEnd('/');

            var endpoint = $"{baseUrl}/v1beta/{settings.Model}:generateContent";
            return includeApiKey ? $"{endpoint}?key={settings.ApiKey}" : endpoint;
        }

        private Task<HttpResponseMessage> SendPayloadAsync(ChatBotSettings settings, string endpoint, RequestModel payload, CancellationToken cancellationToken = default)
        {
            if (HasServiceAccountCredentials(settings))
            {
                var content = JsonContent.Create(payload);
                return _googleApiClient.SendAsync(endpoint, HttpMethod.Post, settings.Scope, settings.ServiceAccountJsonPath, content, cancellationToken);
            }

            return _http.PostAsJsonAsync(endpoint, payload, cancellationToken);
        }
    }
}
