using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Middleware;

public class TelegramAuthMiddleware(
    RequestDelegate next,
    ILogger<TelegramAuthMiddleware> logger)
{
    // Routes that don't need auth
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

        // Skip auth for public routes
        if (PublicRoutes.Contains(path))
        {
            await next(context);
            return;
        }

        // Get initData from header or body
        var initData = await ExtractInitDataAsync(context);

        // ── DEBUG MODE ────────────────────────────────
        // Empty initData in development = bypass auth
        // Returns first user in DB automatically
        // Same behaviour as SwiftyEx Postman collection
        var isDevelopment = context.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment();

        if (string.IsNullOrWhiteSpace(initData) && isDevelopment)
        {
            logger.LogWarning(
                "DEBUG: No initData provided. " +
                "Bypassing auth in development mode.");

            // Set a default test TelegramId for development
            context.Items["TelegramId"] = 123456789L;
            context.Items["InitData"]   = "";

            await next(context);
            return;
        }

        // ── PRODUCTION — Validate initData ────────────
        if (string.IsNullOrWhiteSpace(initData))
        {
            logger.LogWarning(
                "Request rejected — no initData provided.");

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Unauthorized. initData is required."
            });
            return;
        }

        // Validate HMAC-SHA256 hash
        var isValid = telegramService.ValidateInitData(initData);

        if (!isValid)
        {
            logger.LogWarning(
                "Request rejected — invalid initData hash. " +
                "Path: {Path}", path);

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

        // ✅ Auth passed — attach to context
        context.Items["TelegramId"] = telegramId.Value;
        context.Items["InitData"]   = initData;

        logger.LogInformation(
            "Auth passed for TelegramId: {TelegramId}",
            telegramId.Value);

        await next(context);
    }

    // ── Extract initData ──────────────────────────────
    // Check header first, then request body
    private static async Task<string?> ExtractInitDataAsync(
        HttpContext context)
    {
        // Check X-Init-Data header first
        var headerValue = context.Request.Headers["X-Init-Data"]
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(headerValue))
            return headerValue;

        // Check request body for POST requests
        if (context.Request.Method != HttpMethod.Get.Method
            && context.Request.ContentLength > 0)
        {
            // Enable buffering so we can read body twice
            context.Request.EnableBuffering();

            try
            {
                var body = await new StreamReader(
                    context.Request.Body,
                    leaveOpen: true)
                    .ReadToEndAsync();

                // Reset stream position for controller
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
            catch
            {
                // Ignore parse errors
            }
        }

        return null;
    }
}