using Microsoft.AspNetCore.Mvc;
using SwiftyAutopilot.Models;
using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(
    IUserService userService) : ControllerBase
{
    // GET api/users/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        // TelegramId is set by TelegramAuthMiddleware
        var telegramId = GetTelegramId();

        var user = await userService.GetUserAsync(telegramId);

        if (user is null)
            return NotFound(new
            {
                message = "User not found."
            });

        return Ok(user);
    }

    // POST api/users/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var telegramId = GetTelegramId();

        var user = await userService.RegisterOrLoginAsync(
            telegramId,
            request.FirstName,
            request.Username);

        return Ok(user);
    }

    // PUT api/users/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var telegramId = GetTelegramId();

        try
        {
            var user = await userService.UpdateUserAsync(
                telegramId,
                request.FirstName,
                request.Username);

            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // DELETE api/users/me
    [HttpDelete("me")]
    public async Task<IActionResult> DeactivateMe()
    {
        var telegramId = GetTelegramId();

        var result = await userService
            .DeactivateUserAsync(telegramId);

        if (!result)
            return NotFound(new
            {
                message = "User not found."
            });

        return Ok(new
        {
            message = "Account deactivated successfully."
        });
    }

    // ── Private Helpers ───────────────────────────────
    private long GetTelegramId()
    {
        // Set by TelegramAuthMiddleware
        return (long)HttpContext.Items["TelegramId"]!;
    }
}

// ── Request DTOs ──────────────────────────────────────
public record RegisterRequest(
    string FirstName,
    string? Username);

public record UpdateUserRequest(
    string FirstName,
    string? Username);