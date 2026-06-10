using Microsoft.AspNetCore.Mvc;

using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RulesController(
    IRuleService ruleService) : ControllerBase
{
    // GET api/rules
    [HttpGet]
    public async Task<IActionResult> GetMyRules()
    {
        var telegramId = GetTelegramId();

        var rules = await ruleService
            .GetUserRulesAsync(telegramId);

        return Ok(rules);
    }

    // GET api/rules/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRule(Guid id)
    {
        var telegramId = GetTelegramId();

        try
        {
            var rule = await ruleService.GetRuleAsync(id);

            if (rule is null)
                return NotFound(new
                {
                    message = "Rule not found."
                });

            // Security — ensure rule belongs to user
            if (rule.UserId != telegramId)
                return Forbid();

            return Ok(rule);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    // POST api/rules
    [HttpPost]
    public async Task<IActionResult> CreateRule(
        [FromBody] CreateRuleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var telegramId = GetTelegramId();

        try
        {
            var rule = await ruleService
                .CreateRuleAsync(telegramId, request);

            return CreatedAtAction(
                nameof(GetRule),
                new { id = rule.Id },
                rule);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT api/rules/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRule(
        Guid id,
        [FromBody] UpdateRuleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var telegramId = GetTelegramId();

        try
        {
            var rule = await ruleService
                .UpdateRuleAsync(id, telegramId, request);

            return Ok(rule);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PATCH api/rules/{id}/toggle
    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> ToggleRule(Guid id)
    {
        var telegramId = GetTelegramId();

        try
        {
            var result = await ruleService
                .ToggleRuleAsync(id, telegramId);

            return Ok(new
            {
                success = result,
                message = result
                    ? "Rule toggled successfully."
                    : "Rule not found."
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    // DELETE api/rules/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        var telegramId = GetTelegramId();

        try
        {
            var result = await ruleService
                .DeleteRuleAsync(id, telegramId);

            if (!result)
                return NotFound(new
                {
                    message = "Rule not found."
                });

            return Ok(new
            {
                message = "Rule deleted successfully."
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    // ── Private Helpers ───────────────────────────────
    private long GetTelegramId()
    {
        return (long)HttpContext.Items["TelegramId"]!;
    }
}