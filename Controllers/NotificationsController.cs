using Microsoft.AspNetCore.Mvc;
using SwiftyAutopilot.Repositories.Interfaces;

namespace SwiftyAutopilot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController(
    ILogRepository logRepository) : ControllerBase
{
    // GET api/notifications
    // Returns recent execution logs for current user
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int days = 7)
    {
        var telegramId = GetTelegramId();

        var logs = await logRepository
            .GetRecentByUserIdAsync(telegramId, days);

        var result = logs.Select(l => new
        {
            id              = l.Id,
            ruleId          = l.RuleId,
            executedAt      = l.ExecutedAt,
            success         = l.Success,
            message         = l.Message,
            amountProcessed = l.AmountProcessed,
        });

        return Ok(result);
    }

    private long GetTelegramId() =>
        (long)HttpContext.Items["TelegramId"]!;
}