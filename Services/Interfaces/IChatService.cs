namespace SwiftyAutopilot.Services.Interfaces;

public interface IChatService
{
    Task<string> ChatAsync(
        long userId,
        string message,
        List<ChatMessage> history);
}

public record ChatMessage(string Role, string Content);