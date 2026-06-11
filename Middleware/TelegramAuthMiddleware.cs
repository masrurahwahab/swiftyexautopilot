using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Middleware;

public class TelegramAuthMiddleware(
    RequestDelegate next,
    ILogger<TelegramAuthMiddleware> logger)
{
    private static readonly HashSet<string> PublicRoutes = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "/api/swiftyex/rates",
        "/health"
    };

    public async Task InvokeAsync(
        HttpContext context,
        ITelegramService telegramService)
    {
        var path = context.Request.Path.Value ?? "";

        if (PublicRoutes.Contains(path))
        {
            await next(context);
            return;
        }

        var initData = await ExtractInitDataAsync(context);

        // Always require initData — no bypass
        if (string.IsNullOrWhiteSpace(initData))
        {
            logger.LogWarning(
                "Request rejected — no initData provided. Path: {Path}", path);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Unauthorized. Open this app in Telegram."
            });
            return;
        }

        // Validate HMAC-SHA256 hash
        var isValid = telegramService.ValidateInitData(initData);

        if (!isValid)
        {
            logger.LogWarning(
                "Request rejected — invalid initData hash. Path: {Path}", path);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Unauthorized. Invalid Telegram session."
            });
            return;
        }

        // Extract Telegram user ID
        var telegramId = telegramService.ExtractUserId(initData);

        if (telegramId is null)
        {
            logger.LogWarning(
                "Request rejected — could not extract user ID.");

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Unauthorized. Could not identify user."
            });
            return;
        }

        // ✅ Auth passed
        context.Items["TelegramId"] = telegramId.Value;
        context.Items["InitData"]   = initData;

        logger.LogInformation(
            "Auth passed for TelegramId: {TelegramId}", telegramId.Value);

        await next(context);
    }

    private static async Task<string?> ExtractInitDataAsync(
        HttpContext context)
    {
        var headerValue = context.Request.Headers["X-Init-Data"]
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(headerValue))
            return headerValue;

        if (context.Request.Method != HttpMethod.Get.Method
            && context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();

            try
            {
                var body = await new StreamReader(
                    context.Request.Body,
                    leaveOpen: true)
                    .ReadToEndAsync();

                context.Request.Body.Position = 0;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    var doc = System.Text.Json.JsonDocument
                        .Parse(body);

                    if (doc.RootElement.TryGetProperty(
                            "initData", out var initDataEl))
                        return initDataEl.GetString();
                }
            }
            catch { }
        }

        return null;
    }
}
