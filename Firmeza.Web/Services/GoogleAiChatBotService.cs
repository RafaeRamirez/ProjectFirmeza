using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;

namespace Firmeza.Web.Services
{
    public class GoogleAiChatBotService : IChatBotService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;
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

        private const string BusinessPolicies = """
Eres "Firmeza AI", asistente virtual de Firmeza, una plataforma B2B que distribuye productos de inventario controlado (línea hogar, retail y suministros operativos) para clientes corporativos en Latinoamérica. Tu trabajo es acelerar preventa, posventa y soporte interno.

Contexto del negocio:
- Firmeza vende por lotes desde 25 unidades, maneja stock comprometido y promete despachos nacionales en 72h.
- Existen tres frentes claves: disponibilidad de inventario, seguimiento de solicitudes/ventas y recomendaciones para mejorar métricas comerciales (ticket promedio, rotación, fill rate).
- Información crítica: producto, cantidad, urgencia, ciudad de entrega, método de pago (transferencia o crédito 15 días), etapa del cliente y responsable comercial.

Políticas de conversación:
1. Responde siempre en español claro, tono consultivo y profesional.
2. Si faltan datos, explica qué necesitas (producto, volumen, fechas, etc.) antes de comprometer precio o plazo.
3. Nunca inventes números de pedido ni stock; invita a revisar el panel Dashboard/Sales si se requieren folios exactos.
4. Si la consulta no pertenece al negocio de Firmeza, acláralo y redirige a temas válidos (inventario, ventas, soporte, cobranzas, logística).
5. Propón acciones viables dentro de los procesos existentes (crear solicitud, actualizar inventario, preparar propuesta, escalar al ejecutivo).
6. Cierra SIEMPRE con una "Pregunta de negocio" que impulse el siguiente paso o valide información faltante.

Formato recomendado de respuesta:
- Diagnóstico breve (qué entendiste y estado actual).
- Recomendaciones/acciones (máximo 3, numeradas o en viñetas).
- "Pregunta de negocio: ... ?" alineada al objetivo del cliente.

Preguntas guía internas para ti (usa solo si aplica): ¿qué volumen?, ¿qué fecha límite?, ¿qué ciudad?, ¿cliente nuevo o recurrente?, ¿qué KPI quiere mejorar?
""";

        private const string DefaultModel = "models/gemini-1.5-flash";
        private const string DefaultScope = "https://www.googleapis.com/auth/generative-language";

        public GoogleAiChatBotService(
            HttpClient http,
            IChatBotSettingsProvider settingsProvider,
            ILogger<GoogleAiChatBotService> logger)
        {
            _http = http;
            _settingsProvider = settingsProvider;
            _logger = logger;
        }

        public async Task<string> AskAsync(string message, string? userId)
        {
            var settings = await _settingsProvider.GetAsync();
            if (settings is null)
                return "El chatbot no está configurado en la base de datos.";

            if (!HasServiceAccountCredentials(settings) && string.IsNullOrWhiteSpace(settings.ApiKey))
                return "El chatbot no está configurado. Agrega tu API key o credenciales de Service Account.";

            var endpoint = BuildEndpoint(settings);
            var prompt = BuildPrompt(message, userId);
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
                                text = prompt
                            }
                        }
                    }
                }
            };

            try
            {
                var jsonPayload = JsonSerializer.Serialize(payload);
                var requestResult = await CreateHttpRequestAsync(settings, endpoint, jsonPayload, CancellationToken.None);
                if (!requestResult.ok)
                    return requestResult.error!;

                using var response = await _http.SendAsync(requestResult.request!, CancellationToken.None);
                var rawPayload = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini returned {Status} - {Body}", response.StatusCode, rawPayload);
                    return BuildFriendlyError(response.StatusCode, rawPayload);
                }

                var data = JsonSerializer.Deserialize<ResponseModel>(rawPayload, SerializerOptions);
                var text = data?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text;
                return string.IsNullOrWhiteSpace(text) ? "No pude generar una respuesta en este momento." : text.Trim();
            }
            catch (System.Exception ex) when (ex is HttpRequestException or InvalidOperationException or FileNotFoundException)
            {
                _logger.LogError(ex, "Error al consultar la API de Google AI Studio.");
                return "No pude conectar con el servicio de IA. Revisa las credenciales configuradas o tu conexión.";
            }
        }

        private static string NormalizeModel(string? model)
        {
            var value = string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim();
            return value.StartsWith("models/", StringComparison.OrdinalIgnoreCase)
                ? value
                : $"models/{value}";
        }

        private static string BuildPrompt(string message, string? userId)
        {
            var caller = string.IsNullOrWhiteSpace(userId) ? "Invitado" : userId;
            return $"""
{BusinessPolicies}

Usuario autenticado: {caller}.

Mensaje del usuario:
{message}

Recuerda seguir el formato indicado y finalizar con una pregunta de negocio alineada al objetivo del cliente.
""";
        }

        private static string BuildFriendlyError(HttpStatusCode statusCode, string body)
        {
            var code = (int)statusCode;
            return code switch
            {
                429 => "Estamos a tope ahora mismo (límite de la API). Intento automático en segundos, por favor intenta de nuevo si la espera continúa.",
                401 => "Clave inválida o sin permisos. Revisa GEMINI_API_KEY y habilita la Generative Language API.",
                404 => "Modelo o endpoint incorrecto. Usa 'gemini-1.5-flash' y la ruta ':generateContent'.",
                _ => $"Error {code}: {body}"
            };
        }

        private static bool HasServiceAccountCredentials(ChatBotSettings settings)
            => !string.IsNullOrWhiteSpace(settings.ServiceAccountJsonPath);

        private static string ResolveScope(ChatBotSettings settings)
            => string.IsNullOrWhiteSpace(settings.Scope) ? DefaultScope : settings.Scope;

        private static string BuildEndpoint(ChatBotSettings settings)
        {
            var baseUrl = string.IsNullOrWhiteSpace(settings.Endpoint)
                ? "https://generativelanguage.googleapis.com"
                : settings.Endpoint.TrimEnd('/');

            var model = NormalizeModel(settings.Model);
            return $"{baseUrl}/v1beta/{model}:generateContent";
        }

        private async Task<(bool ok, HttpRequestMessage? request, string? error)> CreateHttpRequestAsync(
            ChatBotSettings settings,
            string endpoint,
            string payloadJson,
            CancellationToken cancellationToken)
        {
            if (HasServiceAccountCredentials(settings))
            {
                var saPath = settings.ServiceAccountJsonPath!;
                if (!File.Exists(saPath))
                    return (false, null, $"El archivo de Service Account no existe: {saPath}");

                var scopeValue = ResolveScope(settings);
                var scopes = scopeValue
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (scopes.Length == 0)
                    scopes = new[] { DefaultScope };
                try
                {
                    var credential = GoogleCredential
                        .FromFile(saPath)
                        .CreateScoped(scopes);

                    var token = await credential.UnderlyingCredential
                        .GetAccessTokenForRequestAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                    return (true, request, null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo obtener token OAuth con la Service Account.");
                    return (false, null, "Error al autenticar con la Service Account. Verifica el archivo y permisos de la API.");
                }
            }

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
                return (false, null, "El chatbot no está configurado. Define GOOGLE_APPLICATION_CREDENTIALS o GEMINI_API_KEY.");

            var endpointWithKey = $"{endpoint}?key={settings.ApiKey}";
            var apiRequest = new HttpRequestMessage(HttpMethod.Post, endpointWithKey)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            };
            return (true, apiRequest, null);
        }
    }
}
