using System.Text;
using System.Text.Json;
using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Services;

public class ChatService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IChatService
{
    public async Task<string> ChatAsync(
        long userId,
        string message,
        List<ChatMessage> history)
    {
        var apiKey = configuration["Groq:ApiKey"]
            ?? throw new InvalidOperationException(
                "Groq:ApiKey missing.");

        var client = httpClientFactory.CreateClient("Groq");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add(
            "Authorization", $"Bearer {apiKey}");

        // Build message history
        var messages = new List<object>
        {
            new
            {
                role    = "system",
                content =
                    "You are Swifty AI, a smart financial " +
                    "assistant for SwiftyEx — a Nigerian " +
                    "crypto trading platform. You help users " +
                    "with crypto trading, savings goals, " +
                    "automation rules, and financial advice. " +
                    "You are friendly, concise, and speak " +
                    "naturally. You understand Nigerian " +
                    "context (NGN, Naira, USDT rates). " +
                    "Keep responses under 150 words. " +
                    "Use emojis occasionally. " +
                    "When users ask about automation rules, " +
                    "suggest specific IF/THEN rules they " +
                    "can create on SwiftyEx Autopilot. " +
                    "Never refuse to answer. Always be helpful."
            }
        };

        // Add conversation history
        foreach (var h in history.TakeLast(10))
        {
            messages.Add(new
            {
                role    = h.Role,
                content = h.Content
            });
        }

        // Add current message
        messages.Add(new
        {
            role    = "user",
            content = message
        });

        var requestBody = new
        {
            model       = "llama-3.3-70b-versatile",
            messages,
            temperature = 0.7,
            max_tokens  = 300,
        };

        var json    = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(
            json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            "https://api.groq.com/openai/v1/chat/completions",
            content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content
                .ReadAsStringAsync();
            throw new HttpRequestException(
                $"Groq error: {error}");
        }

        var responseJson = await response.Content
            .ReadAsStringAsync();

        var doc = JsonDocument.Parse(responseJson);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "Sorry, I could not respond.";
    }
}