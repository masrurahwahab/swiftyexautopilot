using SwiftyAutopilot.Models;
using SwiftyAutopilot.Models.Enums;

namespace SwiftyAutopilot.Repositories.Interfaces;

public interface IRuleRepository
{
    // Get single rule by ID
    Task<AutopilotRule?> GetByIdAsync(Guid id);
    
    // Get all rules for a user
    Task<IEnumerable<AutopilotRule>> GetByUserIdAsync(long userId);
    
    // Get all active rules (for rule engine)
    Task<IEnumerable<AutopilotRule>> GetAllActiveAsync();
    
    // Get active rules by trigger type (for rule engine)
    Task<IEnumerable<AutopilotRule>> GetActiveByTriggerTypeAsync(
        TriggerType triggerType);
    
    // Create new rule
    Task<AutopilotRule> CreateAsync(AutopilotRule rule);
    
    // Update existing rule
    Task<AutopilotRule> UpdateAsync(AutopilotRule rule);
    
    // Toggle rule on/off
    Task<bool> ToggleActiveAsync(Guid id);
    
    // Delete rule
    Task<bool> DeleteAsync(Guid id);
    
    // Check if rule belongs to user (security)
    Task<bool> BelongsToUserAsync(Guid ruleId, long userId);
}