namespace SwiftyAutopilot.Models.Enums;

public enum TriggerType
{
    RateHitsTarget,       // When USDT hits ₦X
    PriceDropsPercent,    // When BTC drops X%
    ScheduledTime,        // Cron-based schedule
    BalanceExceeds,       // When balance > ₦X
}