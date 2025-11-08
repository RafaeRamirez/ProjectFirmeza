using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models.ViewModels;

namespace Firmeza.Web.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatBotService _chat;

        public ChatController(IChatBotService chat)
        {
            _chat = chat;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [AllowAnonymous]
        [HttpGet("health")]
        public async Task<IActionResult> Health([FromServices] IHttpClientFactory httpFactory, [FromServices] IConfiguration cfg)
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                         ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
                         ?? cfg["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return BadRequest(new { ok = false, error = "GEMINI_API_KEY not found" });

            var model = Environment.GetEnvironmentVariable("GEMINI_MODEL")
                        ?? cfg["Gemini:Model"]
                        ?? "models/gemini-1.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = "ping" } }
                    }
                }
            };

            var client = httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            try
            {
                using var response = await client.PostAsJsonAsync(url, body);
                var payload = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new
                {
                    ok = response.IsSuccessStatusCode,
                    status = response.StatusCode.ToString(),
                    body = payload
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Mensaje vacío" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var answer = await _chat.AskAsync(request.Message, userId);
            return Ok(new { reply = answer });
        }
    }
}
