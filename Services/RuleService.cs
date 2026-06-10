using SwiftyAutopilot.Models;
using SwiftyAutopilot.Models.Enums;
using SwiftyAutopilot.Repositories.Interfaces;
using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Services;

public class RuleService(
    IRuleRepository ruleRepository,
    ILogRepository  logRepository) : IRuleService
{
    public async Task<AutopilotRule?> GetRuleAsync(Guid id)
    {
        return await ruleRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<AutopilotRule>> GetUserRulesAsync(
        long userId)
    {
        return await ruleRepository.GetByUserIdAsync(userId);
    }

    public async Task<IEnumerable<AutopilotRule>> GetAllActiveRulesAsync()
    {
        return await ruleRepository.GetAllActiveAsync();
    }

    public async Task<AutopilotRule> CreateRuleAsync(
        long userId,
        CreateRuleRequest request)
    {
        // Validate before saving
        ValidateRequest(request);

        var rule = new AutopilotRule
        {
            UserId            = userId,
            Name              = request.Name,
            TriggerType       = request.TriggerType,
            ActionType        = request.ActionType,

            // Trigger config
            TargetAsset       = request.TargetAsset,
            TargetPrice       = request.TargetPrice,
            PriceDropPercent  = request.PriceDropPercent,
            BalanceThreshold  = request.BalanceThreshold,
            CronExpression    = request.CronExpression,

            // Action config
            ActionAsset       = request.ActionAsset,
            ActionAmount      = request.ActionAmount,
            ActionCurrency    = request.ActionCurrency,
            BankAccount       = request.BankAccount,
            BillType          = request.BillType,

            IsActive          = true,
            CreatedAt         = DateTime.UtcNow,
            TimesTriggered    = 0
        };

        return await ruleRepository.CreateAsync(rule);
    }

    public async Task<AutopilotRule> UpdateRuleAsync(
        Guid id,
        long userId,
        UpdateRuleRequest request)
    {
        // Security — rule must belong to user
        var rule = await GetAndVerifyOwnershipAsync(id, userId);

        // Only update fields that are provided
        if (request.Name is not null)
            rule.Name = request.Name;

        if (request.IsActive is not null)
            rule.IsActive = request.IsActive.Value;

        if (request.TargetPrice is not null)
            rule.TargetPrice = request.TargetPrice;

        if (request.PriceDropPercent is not null)
            rule.PriceDropPercent = request.PriceDropPercent;

        if (request.BalanceThreshold is not null)
            rule.BalanceThreshold = request.BalanceThreshold;

        if (request.CronExpression is not null)
            rule.CronExpression = request.CronExpression;

        if (request.ActionAmount is not null)
            rule.ActionAmount = request.ActionAmount;

        if (request.BankAccount is not null)
            rule.BankAccount = request.BankAccount;

        if (request.BillType is not null)
            rule.BillType = request.BillType;

        return await ruleRepository.UpdateAsync(rule);
    }

    public async Task<bool> ToggleRuleAsync(Guid id, long userId)
    {
        await GetAndVerifyOwnershipAsync(id, userId);
        return await ruleRepository.ToggleActiveAsync(id);
    }

    public async Task<bool> DeleteRuleAsync(Guid id, long userId)
    {
        await GetAndVerifyOwnershipAsync(id, userId);
        return await ruleRepository.DeleteAsync(id);
    }

    public async Task MarkTriggeredAsync(Guid id)
    {
        var rule = await ruleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException(
                $"Rule {id} not found.");

        rule.LastTriggeredAt = DateTime.UtcNow;
        rule.TimesTriggered++;

        await ruleRepository.UpdateAsync(rule);
    }

    // ── Private Helpers ───────────────────────────────
    private async Task<AutopilotRule> GetAndVerifyOwnershipAsync(
        Guid ruleId,
        long userId)
    {
        var rule = await ruleRepository.GetByIdAsync(ruleId)
            ?? throw new KeyNotFoundException(
                $"Rule {ruleId} not found.");

        if (rule.UserId != userId)
            throw new UnauthorizedAccessException(
                "You do not own this rule.");

        return rule;
    }

    private static void ValidateRequest(CreateRuleRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException(
                "Rule name is required.");

        // Trigger validation
        switch (req.TriggerType)
        {
            case TriggerType.RateHitsTarget:
                if (req.TargetPrice is null or <= 0)
                    throw new ArgumentException(
                        "Target price required.");
                if (string.IsNullOrWhiteSpace(req.TargetAsset))
                    throw new ArgumentException(
                        "Target asset required.");
                break;

            case TriggerType.PriceDropsPercent:
                if (req.PriceDropPercent is null or <= 0 or > 100)
                    throw new ArgumentException(
                        "Price drop percent must be 1–100.");
                break;

            case TriggerType.ScheduledTime:
                if (string.IsNullOrWhiteSpace(req.CronExpression))
                    throw new ArgumentException(
                        "Cron expression required.");
                break;

            case TriggerType.BalanceExceeds:
                if (req.BalanceThreshold is null or <= 0)
                    throw new ArgumentException(
                        "Balance threshold required.");
                break;
        }

        // Action validation
        switch (req.ActionType)
        {
            case ActionType.SellToBank:
                if (req.ActionAmount is null or <= 0)
                    throw new ArgumentException(
                        "Amount required for SellToBank.");
                if (string.IsNullOrWhiteSpace(req.BankAccount))
                    throw new ArgumentException(
                        "Bank account required for SellToBank.");
                break;

            case ActionType.BuyCrypto:
                if (req.ActionAmount is null or <= 0)
                    throw new ArgumentException(
                        "Amount required for BuyCrypto.");
                if (string.IsNullOrWhiteSpace(req.ActionAsset))
                    throw new ArgumentException(
                        "Asset required for BuyCrypto.");
                break;

            case ActionType.PayBill:
                if (string.IsNullOrWhiteSpace(req.BillType))
                    throw new ArgumentException(
                        "Bill type required for PayBill.");
                break;
        }
    }
}