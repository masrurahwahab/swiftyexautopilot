using Microsoft.EntityFrameworkCore;
using SwiftyAutopilot.Data;
using SwiftyAutopilot.Models;
using SwiftyAutopilot.Models.Enums;
using SwiftyAutopilot.Repositories.Interfaces;

namespace SwiftyAutopilot.Repositories;

public class RuleRepository(AppDbContext db) : IRuleRepository
{
    public async Task<AutopilotRule?> GetByIdAsync(Guid id)
    {
        return await db.Rules
            .Include(r => r.Logs)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<AutopilotRule>> GetByUserIdAsync(long userId)
    {
        return await db.Rules
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AutopilotRule>> GetAllActiveAsync()
    {
        return await db.Rules
            .Where(r => r.IsActive)
            .Include(r => r.User)
            .ToListAsync();
    }

    public async Task<IEnumerable<AutopilotRule>> GetActiveByTriggerTypeAsync(
        TriggerType triggerType)
    {
        return await db.Rules
            .Where(r => r.IsActive && r.TriggerType == triggerType)
            .Include(r => r.User)
            .ToListAsync();
    }

    public async Task<AutopilotRule> CreateAsync(AutopilotRule rule)
    {
        db.Rules.Add(rule);
        await db.SaveChangesAsync();
        return rule;
    }

    public async Task<AutopilotRule> UpdateAsync(AutopilotRule rule)
    {
        db.Rules.Update(rule);
        await db.SaveChangesAsync();
        return rule;
    }

    public async Task<bool> ToggleActiveAsync(Guid id)
    {
        var rule = await db.Rules.FindAsync(id);
        if (rule is null) return false;

        rule.IsActive = !rule.IsActive;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var rule = await db.Rules.FindAsync(id);
        if (rule is null) return false;

        db.Rules.Remove(rule);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> BelongsToUserAsync(Guid ruleId, long userId)
    {
        return await db.Rules
            .AnyAsync(r => r.Id == ruleId && r.UserId == userId);
    }
}