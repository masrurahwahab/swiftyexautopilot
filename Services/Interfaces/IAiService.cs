namespace SwiftyAutopilot.Services.Interfaces;

public interface IAiService
{
    // Take user's plain text goal and return 
    // a list of rule suggestions
    Task<List<AiRuleSuggestion>> SuggestRulesAsync(
        string userGoal);
}

// ── AI Response DTO ───────────────────────────────────
public record AiRuleSuggestion(
    string Name,            // e.g. "Weekly BTC Buy"
    string TriggerType,     // e.g. "ScheduledTime"
    string ActionType,      // e.g. "BuyCrypto"
    string Description,     // e.g. "Every Friday → Buy $12 BTC"

    // Trigger values
    string? CronExpression,
    string? TargetAsset,
    decimal? TargetPrice,
    decimal? PriceDropPercent,
    decimal? BalanceThreshold,

    // Action values
    string? ActionAsset,
    decimal? ActionAmount,
    string? ActionCurrency,
    string? BillType
);