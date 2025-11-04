using Firmeza.WebApplication.Interfaces;
using Firmeza.WebApplication.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.WebApplication.Controllers;

// [Authorize(Roles = "Admin")] // enable when auth is configured
public class ChatController : Controller
{
    private readonly IAiChatService _ai;
    public ChatController(IAiChatService ai) => _ai = ai;

    [HttpGet]
    public IActionResult Index() => View(model: "");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessages.InvalidInput);
            return View(model: "");
        }

        try
        {
            var answer = await _ai.GetAnswerAsync(userMessage, HttpContext.RequestAborted);
            return View(model: answer);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, ErrorMessages.Unexpected);
            return View(model: "");
        }
    }
}
