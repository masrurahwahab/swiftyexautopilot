using SwiftyAutopilot.Models;
using SwiftyAutopilot.Models.Enums;

namespace SwiftyAutopilot.Services.Interfaces;

public interface IRuleService
{
    // Get single rule by ID
    Task<AutopilotRule?> GetRuleAsync(Guid id);
    
    // Get all rules for a user
    Task<IEnumerable<AutopilotRule>> GetUserRulesAsync(long userId);
    
    // Get all active rules (for rule engine)
    Task<IEnumerable<AutopilotRule>> GetAllActiveRulesAsync();
    
    // Create new rule
    Task<AutopilotRule> CreateRuleAsync(long userId, 
        CreateRuleRequest request);
    
    // Update existing rule
    Task<AutopilotRule> UpdateRuleAsync(Guid id, 
        long userId, UpdateRuleRequest request);
    
    // Toggle rule on/off
    Task<bool> ToggleRuleAsync(Guid id, long userId);
    
    // Delete rule
    Task<bool> DeleteRuleAsync(Guid id, long userId);
    
    // Mark rule as triggered
    Task MarkTriggeredAsync(Guid id);
}

// ── DTOs ──────────────────────────────────────────────
public record CreateRuleRequest(
    string Name,
    TriggerType TriggerType,
    ActionType ActionType,

    // Trigger config
    string? TargetAsset,
    decimal? TargetPrice,
    decimal? PriceDropPercent,
    decimal? BalanceThreshold,
    string? CronExpression,

    // Action config
    string? ActionAsset,
    decimal? ActionAmount,
    string? ActionCurrency,
    string? BankAccount,
    string? BillType
);

public record UpdateRuleRequest(
    string? Name,
    bool? IsActive,
    decimal? TargetPrice,
    decimal? PriceDropPercent,
    decimal? BalanceThreshold,
    string? CronExpression,
    decimal? ActionAmount,
    string? BankAccount,
    string? BillType
);