namespace SwiftyAutopilot.Services.Interfaces;

public interface IRuleEngineService
{
    // Called every 30 seconds by Hangfire
    Task RunAsync();

    // Check rate-based rules
    Task CheckRateRulesAsync(
        Dictionary<string, decimal> currentRates);

    // Check scheduled rules (cron)
    Task CheckScheduledRulesAsync();

    // Check balance rules
    Task CheckBalanceRulesAsync();

    // Execute a specific rule
    Task ExecuteRuleAsync(
        Models.AutopilotRule rule,
        string triggerReason);
}