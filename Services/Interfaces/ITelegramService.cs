namespace SwiftyAutopilot.Services.Interfaces;

public interface ITelegramService
{
    // Send a simple text message to a user
    Task SendMessageAsync(long telegramId, string message);

    // Send rule triggered notification
    Task SendRuleTriggeredAsync(long telegramId,
        string ruleName, string resultMessage);

    // Send rule execution failed notification
    Task SendRuleFailedAsync(long telegramId,
        string ruleName, string reason);

    // Validate Telegram initData from Mini App
    // Returns true if data is genuine from Telegram
    bool ValidateInitData(string initData);

    // Extract Telegram user ID from initData
    long? ExtractUserId(string initData);
    
    
}