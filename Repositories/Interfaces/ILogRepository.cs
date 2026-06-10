using SwiftyAutopilot.Models;

namespace SwiftyAutopilot.Repositories.Interfaces;

public interface ILogRepository
{
    // Get all logs for a rule
    Task<IEnumerable<ExecutionLog>> GetByRuleIdAsync(Guid ruleId);
    
    // Get all logs for a user
    Task<IEnumerable<ExecutionLog>> GetByUserIdAsync(long userId);
    
    // Get recent logs for a user (last X days)
    Task<IEnumerable<ExecutionLog>> GetRecentByUserIdAsync(
        long userId, int days = 7);
    
    // Create new log entry
    Task<ExecutionLog> CreateAsync(ExecutionLog log);
    
    // Count successful executions for a rule
    Task<int> CountSuccessfulAsync(Guid ruleId);
    
    // Get total amount processed for a user
    Task<decimal> GetTotalAmountProcessedAsync(long userId);
}