using Microsoft.AspNetCore.Mvc;
using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StreakController(
    IStreakService streakService) : ControllerBase
{
    // POST api/streak/open
    // Call this when user opens the app
    [HttpPost("open")]
    public async Task<IActionResult> RecordOpen()
    {
        var telegramId = GetTelegramId();
        var result     = await streakService
            .RecordOpenAsync(telegramId);
        return Ok(result);
    }

    // GET api/streak
    // Get current streak info
    [HttpGet]
    public async Task<IActionResult> GetStreak()
    {
        var telegramId = GetTelegramId();
        var result     = await streakService
            .GetStreakAsync(telegramId);
        return Ok(result);
    }

    private long GetTelegramId() =>
        (long)HttpContext.Items["TelegramId"]!;
}