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
