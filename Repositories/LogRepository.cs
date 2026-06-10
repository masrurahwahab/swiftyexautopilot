using Microsoft.EntityFrameworkCore;
using SwiftyAutopilot.Data;
using SwiftyAutopilot.Models;
using SwiftyAutopilot.Repositories.Interfaces;

namespace SwiftyAutopilot.Repositories;

public class LogRepository(AppDbContext db) : ILogRepository
{
    public async Task<IEnumerable<ExecutionLog>> GetByRuleIdAsync(Guid ruleId)
    {
        return await db.ExecutionLogs
            .Where(l => l.RuleId == ruleId)
            .OrderByDescending(l => l.ExecutedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ExecutionLog>> GetByUserIdAsync(long userId)
    {
        return await db.ExecutionLogs
            .Where(l => l.Rule.UserId == userId)
            .OrderByDescending(l => l.ExecutedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ExecutionLog>> GetRecentByUserIdAsync(
        long userId, int days = 7)
    {
        var from = DateTime.UtcNow.AddDays(-days);

        return await db.ExecutionLogs
            .Where(l => l.Rule.UserId == userId 
                        && l.ExecutedAt >= from)
            .OrderByDescending(l => l.ExecutedAt)
            .ToListAsync();
    }

    public async Task<ExecutionLog> CreateAsync(ExecutionLog log)
    {
        db.ExecutionLogs.Add(log);
        await db.SaveChangesAsync();
        return log;
    }

    public async Task<int> CountSuccessfulAsync(Guid ruleId)
    {
        return await db.ExecutionLogs
            .CountAsync(l => l.RuleId == ruleId && l.Success);
    }

    public async Task<decimal> GetTotalAmountProcessedAsync(long userId)
    {
        return await db.ExecutionLogs
            .Where(l => l.Rule.UserId == userId 
                        && l.Success 
                        && l.AmountProcessed.HasValue)
            .SumAsync(l => l.AmountProcessed!.Value);
    }
}