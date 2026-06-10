using Microsoft.AspNetCore.Mvc;
using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(
    IChatService chatService) : ControllerBase
{
    // POST api/chat
    [HttpPost]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new
            {
                message = "Message cannot be empty."
            });

        var telegramId = GetTelegramId();

        try
        {
            var reply = await chatService.ChatAsync(
                telegramId,
                request.Message,
                request.History ?? new List<ChatMessage>());

            return Ok(new { reply });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Chat service error.",
                detail  = ex.Message
            });
        }
    }

    private long GetTelegramId() =>
        (long)HttpContext.Items["TelegramId"]!;
}

// ── DTOs ──────────────────────────────────────────────
public record ChatRequest(
    string Message,
    List<ChatMessage>? History);