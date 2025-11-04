using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Firmeza.WebApplication.Interfaces;

namespace Firmeza.WebApplication.Services;

// Minimal Google AI Studio client (HTTP). Replace with full-featured later if needed.
public sealed class GeminiAiService : IAiChatService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiAiService(HttpClient http, IConfiguration cfg)
    {
        _http = http;

        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                 ?? cfg["GEMINI_API_KEY"]
                 ?? throw new InvalidOperationException("Missing GEMINI_API_KEY");

        _model = Environment.GetEnvironmentVariable("GEMINI_MODEL")
                 ?? cfg["GEMINI_MODEL"]
                 ?? "gemini-2.0-flash";

        _http.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public Task<string> GetAnswerAsync(string prompt, System.Threading.CancellationToken cancellationToken = default)
        => AskAsync(prompt, cancellationToken);

    private async Task<string> AskAsync(string userMessage, System.Threading.CancellationToken ct = default)
    {
        var systemInstruction = new
        {
            parts = new[] { new { text =
@"You are Firmeza's assistant.
Scope: Products, Customers, Sales, Receipts, Store Policies.
Style: concise, friendly, Spanish responses; prices in COP; avoid hallucinations.
If unknown/out of scope, say you don't know and suggest contacting Admin." } }
        };

        var body = new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = userMessage } } } },
            system_instruction = systemInstruction
        };

        var url = $"v1beta/models/{_model}:generateContent?key={_apiKey}";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };

        using var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            var problem = await res.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Gemini API error: {(int)res.StatusCode} - {problem}");
        }

        using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return "Lo siento, no tengo respuesta en este momento.";

        var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        return string.IsNullOrWhiteSpace(text) ? "Lo siento, no tengo respuesta disponible." : text!;
    }
}
