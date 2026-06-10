using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Hangfire;
using Telegram.Bot;
using SwiftyAutopilot.Data;
using SwiftyAutopilot.Middleware;
using SwiftyAutopilot.Repositories;
using SwiftyAutopilot.Repositories.Interfaces;
using SwiftyAutopilot.Services;
using SwiftyAutopilot.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
// ── Hangfire ──────────────────────────────────────────
builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

// ── Controllers ───────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions
            .PropertyNameCaseInsensitive = true;

        options.JsonSerializerOptions
                .PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;

        // ── Fix circular reference ────────────────
        options.JsonSerializerOptions
                .ReferenceHandler =
            System.Text.Json.Serialization
                .ReferenceHandler.IgnoreCycles;
    });
// ✅ Add this
var connectionString = builder.Configuration
                           .GetConnectionString("Default")
                       ?? throw new InvalidOperationException(
                           "Connection string not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0))));

// ── Telegram Bot Client ───────────────────────────────
builder.Services.AddSingleton<ITelegramBotClient>(_ =>
    new TelegramBotClient(
        builder.Configuration["Telegram:BotToken"]
        ?? throw new InvalidOperationException(
            "Telegram:BotToken not configured.")));

// ── HTTP Clients ──────────────────────────────────────
builder.Services.AddHttpClient("Gemini");
builder.Services.AddHttpClient("SwiftyEx");

// Groq client with SSL handler
builder.Services.AddHttpClient("Groq")
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler
                    .DangerousAcceptAnyServerCertificateValidator
        });

// ── Repositories ──────────────────────────────────────
builder.Services.AddScoped<IUserRepository,  UserRepository>();
builder.Services.AddScoped<IRuleRepository,  RuleRepository>();
builder.Services.AddScoped<ILogRepository,   LogRepository>();

// ── Services ──────────────────────────────────────────
builder.Services.AddScoped<IUserService,     UserService>();
builder.Services.AddScoped<IRuleService,     RuleService>();
builder.Services.AddScoped<IAiService,       AiService>();
builder.Services.AddScoped<ITelegramService, TelegramService>();
builder.Services.AddScoped<ISwiftyExService, SwiftyExService>();
builder.Services.AddScoped<IStreakService, StreakService>();
builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();
builder.Services.AddScoped<IChatService, ChatService>();

// ── Rate Limiting ─────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(
        policyName: "fixed",
        configureOptions: limiter =>
        {
            limiter.PermitLimit = builder.Configuration
                .GetValue<int>("RateLimit:PermitLimit", 30);

            limiter.Window = TimeSpan.FromSeconds(
                builder.Configuration
                    .GetValue<int>("RateLimit:WindowSeconds", 60));

            limiter.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;

            limiter.QueueLimit = 5;
        });

    // Return 429 when rate limit hit
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many requests. Slow down." },
            token);
    };
});
// ── Telegram Bot Client ───────────────────────────────
var botToken = builder.Configuration["Telegram:BotToken"]
               ?? throw new InvalidOperationException(
                   "Telegram:BotToken is missing from appsettings.json");

builder.Services.AddSingleton<ITelegramBotClient>(_ =>
    new TelegramBotClient(botToken));
// ── CORS ──────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("TelegramMiniApp", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? Array.Empty<string>();

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ── Health Check ──────────────────────────────────────
// ✅ Basic health check — no extra package needed
builder.Services.AddHealthChecks();

// ─────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────

// ── Auto migrate database on startup ──────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ── Middleware Pipeline ───────────────────────────────
// Order matters — do not rearrange

app.UseHttpsRedirection();

app.UseCors("TelegramMiniApp");

app.UseRateLimiter();

// Telegram auth on every request
app.UseMiddleware<TelegramAuthMiddleware>();

app.MapControllers();

app.MapHealthChecks("/health");
// ── Hangfire Dashboard ────────────────────────────────
app.UseHangfireDashboard("/hangfire");

// ── Schedule Rule Engine every 30 seconds ─────────────
RecurringJob.AddOrUpdate<IRuleEngineService>(
    "rule-engine",
    service => service.RunAsync(),
    Cron.Minutely(),
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Utc
    });
app.Run();