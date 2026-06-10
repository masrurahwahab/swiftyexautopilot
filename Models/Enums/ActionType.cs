namespace SwiftyAutopilot.Models.Enums;

public enum ActionType
{
    SellToBank,           // Sell crypto → NGN → bank
    BuyCrypto,            // Buy crypto automatically
    PayBill,              // Pay DSTV/Airtime/Electricity
    SendAlert,            // Notify user via Telegram
    SaveToGoal,           // Move funds to savings goal
}