using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Services;

public class TelegramService(
    ITelegramBotClient botClient,
    IConfiguration configuration) : ITelegramService
{
    public async Task SendMessageAsync(
        long telegramId,
        string message)
    {
        await botClient.SendMessage(
            chatId: telegramId,
            text: message,
            parseMode: ParseMode.Html);
    }

    public async Task SendRuleTriggeredAsync(
        long telegramId,
        string ruleName,
        string resultMessage)
    {
        var message = $"""
            ⚡ <b>Autopilot Triggered</b>

            📌 Rule: <b>{ruleName}</b>
            ✅ {resultMessage}

            <i>Your money is working while you rest.</i>
            """;

        await botClient.SendMessage(
            chatId: telegramId,
            text: message,
            parseMode: ParseMode.Html);
    }

    public async Task SendRuleFailedAsync(
        long telegramId,
        string ruleName,
        string reason)
    {
        var message = $"""
            ⚠️ <b>Autopilot Rule Failed</b>

            📌 Rule: <b>{ruleName}</b>
            ❌ Reason: {reason}

            <i>Please review your rule settings.</i>
            """;

        await botClient.SendMessage(
            chatId: telegramId,
            text: message,
            parseMode: ParseMode.Html);
    }

    public bool ValidateInitData(string initData)
    {
        try
        {
            var botToken = configuration["Telegram:BotToken"]
                ?? throw new InvalidOperationException(
                    "Bot token not configured.");

            // Parse the initData query string
            var parameters = initData
                .Split('&')
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(
                    p => Uri.UnescapeDataString(p[0]),
                    p => Uri.UnescapeDataString(p[1]));

            // Extract and remove hash from parameters
            if (!parameters.TryGetValue("hash", out var hash))
                return false;

            parameters.Remove("hash");

            // Build data check string
            // Must be sorted alphabetically
            var dataCheckString = string.Join(
                "\n",
                parameters
                    .OrderBy(p => p.Key)
                    .Select(p => $"{p.Key}={p.Value}"));

            // HMAC-SHA256 with key = HMAC-SHA256("WebAppData", botToken)
            var secretKey = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes("WebAppData"),
                Encoding.UTF8.GetBytes(botToken));

            var computedHash = HMACSHA256.HashData(
                secretKey,
                Encoding.UTF8.GetBytes(dataCheckString));

            var computedHashHex = Convert.ToHexString(computedHash)
                .ToLower();

            // Constant time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHashHex),
                Encoding.UTF8.GetBytes(hash.ToLower()));
        }
        catch
        {
            return false;
        }
    }

    public long? ExtractUserId(string initData)
    {
        try
        {
            var parameters = initData
                .Split('&')
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(
                    p => Uri.UnescapeDataString(p[0]),
                    p => Uri.UnescapeDataString(p[1]));

            if (!parameters.TryGetValue("user", out var userJson))
                return null;

            var userDoc = JsonDocument.Parse(userJson);

            if (userDoc.RootElement.TryGetProperty(
                    "id", out var idElement))
                return idElement.GetInt64();

            return null;
        }
        catch
        {
            return null;
        }
    }
}