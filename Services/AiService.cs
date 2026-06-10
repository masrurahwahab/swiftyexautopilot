using System.Text;
using System.Text.Json;

using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Services;

public class AiService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IAiService
{
    public async Task<List<AiRuleSuggestion>> SuggestRulesAsync(
        string userGoal)
    {
        var apiKey = configuration["Groq:ApiKey"]
            ?? throw new InvalidOperationException(
                "Groq:ApiKey is missing from appsettings.json");

        var client = httpClientFactory.CreateClient("Groq");

        // Clear and set auth header fresh every call
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add(
            "Authorization", $"Bearer {apiKey}");

        var requestBody = new
        {
            model    = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new
                {
                    role    = "user",
                    content = BuildPrompt(userGoal)
                }
            },
            temperature = 0.3,
            max_tokens  = 1024
        };

        var json    = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync(
            "https://api.groq.com/openai/v1/chat/completions",
            content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content
                .ReadAsStringAsync();

            throw new HttpRequestException(
                $"Groq API error {(int)response.StatusCode}: " +
                $"{errorBody}");
        }

        var responseJson = await response.Content
            .ReadAsStringAsync();

        return ParseGroqResponse(responseJson);
    }

    // ── Private Helpers ───────────────────────────────
    private static string BuildPrompt(string userGoal)
    {
        var exampleJson =
            "[\n" +
            "  {\n" +
            "    \"name\": \"Weekly BTC Buy\",\n" +
            "    \"triggerType\": \"ScheduledTime\",\n" +
            "    \"actionType\": \"BuyCrypto\",\n" +
            "    \"description\": \"Every Friday buy $12 BTC\",\n" +
            "    \"cronExpression\": \"0 18 * * 5\",\n" +
            "    \"targetAsset\": null,\n" +
            "    \"targetPrice\": null,\n" +
            "    \"priceDropPercent\": null,\n" +
            "    \"balanceThreshold\": null,\n" +
            "    \"actionAsset\": \"BTC\",\n" +
            "    \"actionAmount\": 12,\n" +
            "    \"actionCurrency\": \"USD\",\n" +
            "    \"billType\": null\n" +
            "  }\n" +
            "]";

        return
            "You are a financial automation assistant for SwiftyEx, " +
            "a Nigerian crypto trading platform.\n\n" +
            "A user has described their financial goal:\n" +
            "\"" + userGoal + "\"\n\n" +
            "Generate a list of 2-4 automation rules to help them " +
            "achieve this goal using SwiftyEx.\n\n" +
            "Each rule must have:\n" +
            "- name: short rule name\n" +
            "- triggerType: one of [RateHitsTarget, PriceDropsPercent, " +
            "ScheduledTime, BalanceExceeds]\n" +
            "- actionType: one of [SellToBank, BuyCrypto, PayBill, " +
            "SendAlert, SaveToGoal]\n" +
            "- description: one line human-readable summary\n" +
            "- cronExpression: if ScheduledTime (standard cron format)\n" +
            "- targetAsset: crypto symbol if needed e.g. USDT, BTC\n" +
            "- targetPrice: NGN price if RateHitsTarget\n" +
            "- priceDropPercent: number 1-100 if PriceDropsPercent\n" +
            "- balanceThreshold: NGN amount if BalanceExceeds\n" +
            "- actionAsset: crypto symbol for BuyCrypto\n" +
            "- actionAmount: number amount\n" +
            "- actionCurrency: USDT or NGN\n" +
            "- billType: DSTV, AIRTIME, or ELECTRICITY if PayBill\n\n" +
            "Respond ONLY with a valid JSON array. " +
            "No explanation. No markdown. Just the raw JSON array.\n\n" +
            "Example format:\n" +
            exampleJson;
    }

    private static List<AiRuleSuggestion> ParseGroqResponse(
        string responseJson)
    {
        try
        {
            var doc = JsonDocument.Parse(responseJson);

            // Groq follows OpenAI response structure
            var text = doc
                .RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "[]";

            // Clean any accidental markdown fences
            text = text
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var suggestions = JsonSerializer
                .Deserialize<List<AiRuleSuggestion>>(
                    text,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return suggestions ?? new List<AiRuleSuggestion>();
        }
        catch
        {
            return new List<AiRuleSuggestion>();
        }
    }
}