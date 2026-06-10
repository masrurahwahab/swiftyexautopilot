using SwiftyAutopilot.Models;
using SwiftyAutopilot.Models.Enums;
using SwiftyAutopilot.Repositories.Interfaces;
using SwiftyAutopilot.Services.Interfaces;
using Cronos;

namespace SwiftyAutopilot.Services;

public class RuleEngineService(
    IRuleRepository     ruleRepository,
    ILogRepository      logRepository,
    ITelegramService    telegramService,
    IHttpClientFactory  httpClientFactory,
    IConfiguration      configuration,
    ILogger<RuleEngineService> logger)
    : IRuleEngineService
{
    
    // ── Main runner — called every 30 seconds ─────────
    public async Task RunAsync()
    {
        logger.LogInformation(
            "Rule engine running at {Time}",
            DateTime.UtcNow);

        try
        {
            // Fetch current crypto rates
            var rates = await FetchCurrentRatesAsync();

            await CheckRateRulesAsync(rates);
            await CheckScheduledRulesAsync();
            await CheckBalanceRulesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Rule engine error: {Message}",
                ex.Message);
        }
    }

    // ── Check Rate-Based Rules ─────────────────────────
    public async Task CheckRateRulesAsync(
        Dictionary<string, decimal> currentRates)
    {
        var rules = await ruleRepository
            .GetActiveByTriggerTypeAsync(
                TriggerType.RateHitsTarget);

        foreach (var rule in rules)
        {
            try
            {
                if (string.IsNullOrEmpty(rule.TargetAsset)
                    || rule.TargetPrice is null)
                    continue;

                var asset = rule.TargetAsset.ToUpper();

                if (!currentRates.TryGetValue(
                        asset, out var currentRate))
                    continue;

                // Check if rate condition is met
                if (currentRate >= rule.TargetPrice.Value)
                {
                    logger.LogInformation(
                        "Rate rule triggered: {RuleName} " +
                        "— {Asset} at {Rate}",
                        rule.Name, asset, currentRate);

                    await ExecuteRuleAsync(rule,
                        $"{asset} hit ₦{currentRate:N0} " +
                        $"(target: ₦{rule.TargetPrice:N0})");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error checking rate rule {RuleId}",
                    rule.Id);
            }
        }
    }

    // ── Check Scheduled Rules (Cron) ──────────────────
    public async Task CheckScheduledRulesAsync()
    {
        var rules = await ruleRepository
            .GetActiveByTriggerTypeAsync(
                TriggerType.ScheduledTime);

        var now = DateTime.UtcNow;

        foreach (var rule in rules)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rule.CronExpression))
                    continue;

                var cron = CronExpression.Parse(rule.CronExpression);

                // Ensure DateTime is UTC
                var lastRun = rule.LastTriggeredAt ?? rule.CreatedAt;

                if (lastRun.Kind != DateTimeKind.Utc)
                {
                    lastRun = DateTime.SpecifyKind(
                        lastRun,
                        DateTimeKind.Utc);
                }

                var nextRun = cron.GetNextOccurrence(
                    lastRun,
                    TimeZoneInfo.Utc);

                logger.LogInformation(
                    "Rule: {RuleName}, LastRun: {LastRun}, NextRun: {NextRun}",
                    rule.Name,
                    lastRun,
                    nextRun);

                if (nextRun.HasValue && now >= nextRun.Value)
                {
                    logger.LogInformation(
                        "Scheduled rule triggered: {RuleName}",
                        rule.Name);

                    await ExecuteRuleAsync(
                        rule,
                        $"Scheduled trigger fired at {now:HH:mm:ss} UTC");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error checking scheduled rule {RuleId}",
                    rule.Id);
            }
        }
    }
    // ── Check Balance Rules ───────────────────────────
    public async Task CheckBalanceRulesAsync()
    {
        // Balance rules require user wallet data
        // In production this would call SwiftyEx API
        // For hackathon — skip (no auth token available)
        await Task.CompletedTask;
    }

    // ── Execute a Rule ────────────────────────────────
    public async Task ExecuteRuleAsync(
        AutopilotRule rule,
        string triggerReason)
    {
        // Prevent double-firing within 1 minute
        if (rule.LastTriggeredAt.HasValue)
        {
            var lastTriggered = rule.LastTriggeredAt.Value;

            if (lastTriggered.Kind != DateTimeKind.Utc)
            {
                lastTriggered = DateTime.SpecifyKind(
                    lastTriggered,
                    DateTimeKind.Utc);
            }

            if ((DateTime.UtcNow - lastTriggered)
                .TotalMinutes < 1)
            {
                return;
            }
        }

        try
        {
            // Build action summary
            var actionSummary = BuildActionSummary(rule);

            // Log execution
            var log = new ExecutionLog
            {
                RuleId          = rule.Id,
                ExecutedAt      = DateTime.UtcNow,
                Success         = true,
                Message         = $"{triggerReason} → {actionSummary}",
                AmountProcessed = rule.ActionAmount,
            };

            await logRepository.CreateAsync(log);

            // Update rule stats
            rule.LastTriggeredAt = DateTime.UtcNow;
            rule.TimesTriggered++;
            await ruleRepository.UpdateAsync(rule);

            // Send Telegram notification 🔔
            await telegramService.SendRuleTriggeredAsync(
                rule.UserId,
                rule.Name,
                $"{triggerReason}\n\n{actionSummary}");

            logger.LogInformation(
                "Rule executed successfully: {RuleName}",
                rule.Name);
        }
        catch (Exception ex)
        {
            // Log failure
            var failLog = new ExecutionLog
            {
                RuleId     = rule.Id,
                ExecutedAt = DateTime.UtcNow,
                Success    = false,
                Message    = ex.Message,
            };

            await logRepository.CreateAsync(failLog);

            // Send failure notification
            await telegramService.SendRuleFailedAsync(
                rule.UserId,
                rule.Name,
                ex.Message);

            logger.LogError(ex,
                "Rule execution failed: {RuleName}",
                rule.Name);
        }
    }

    // ── Fetch Current Rates from CoinGecko ────────────
    private async Task<Dictionary<string, decimal>>
        FetchCurrentRatesAsync()
    {
        try
        {
            var client = httpClientFactory
                .CreateClient("Groq");

            var ngnPerUsd = 1600m; // Fixed rate for now

            var url = "https://api.coingecko.com/api/v3" +
                      "/simple/price?ids=bitcoin,ethereum," +
                      "tether,binancecoin,solana,ripple" +
                      "&vs_currencies=usd";

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return GetFallbackRates();

            var json = await response.Content
                .ReadAsStringAsync();

            var doc = System.Text.Json.JsonDocument
                .Parse(json);

            var rates = new Dictionary<string, decimal>();

            var coinMap = new Dictionary<string, string>
            {
                { "bitcoin",     "BTC"  },
                { "ethereum",    "ETH"  },
                { "tether",      "USDT" },
                { "binancecoin", "BNB"  },
                { "solana",      "SOL"  },
                { "ripple",      "XRP"  },
            };

            foreach (var (id, symbol) in coinMap)
            {
                if (doc.RootElement.TryGetProperty(
                        id, out var coinEl) &&
                    coinEl.TryGetProperty(
                        "usd", out var usdEl))
                {
                    var usdPrice = usdEl.GetDecimal();
                    // Convert to NGN
                    rates[symbol] = usdPrice * ngnPerUsd;
                }
            }

            return rates;
        }
        catch
        {
            return GetFallbackRates();
        }
    }

    private static Dictionary<string, decimal>
        GetFallbackRates()
    {
        return new Dictionary<string, decimal>
        {
            { "BTC",  108_000_000m },
            { "ETH",    5_600_000m },
            { "USDT",       1_600m },
            { "BNB",      960_000m },
            { "SOL",      275_000m },
            { "XRP",          930m },
        };
    }

    // ── Build action summary text ──────────────────────
    private static string BuildActionSummary(
        AutopilotRule rule)
    {
        return rule.ActionType switch
        {
            ActionType.SellToBank =>
                $"Sell {rule.ActionAmount} {rule.ActionAsset}" +
                $" → NGN → Bank ({rule.BankAccount})",

            ActionType.BuyCrypto =>
                $"Buy {rule.ActionCurrency}" +
                $"{rule.ActionAmount} of {rule.ActionAsset}",

            ActionType.PayBill =>
                $"Pay {rule.BillType} bill" +
                $" (₦{rule.ActionAmount:N0})",

            ActionType.SendAlert =>
                "Alert sent to your Telegram",

            ActionType.SaveToGoal =>
                $"Save ₦{rule.ActionAmount:N0}" +
                $" to your goal",

            _ => "Action executed"
        };
    }
}