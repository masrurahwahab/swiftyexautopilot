namespace SwiftyAutopilot.Services.Interfaces;

public interface ISwiftyExService
{
    // Get user profile from SwiftyEx
    Task<SwiftyExUser?> GetUserProfileAsync(string initData);

    // Get user wallets and balances
    Task<List<SwiftyExWallet>> GetWalletsAsync(string initData);

    // Get transaction history
    Task<List<SwiftyExTransaction>> GetTransactionsAsync(
        string initData,
        int page = 1,
        string? walletType = null);

    // Get current buy/sell rates
    Task<List<SwiftyExRate>> GetRatesAsync();
}

// ── Response DTOs ─────────────────────────────────────
public record SwiftyExUser(
    long ChatId,
    string Username,
    string FirstName,
    bool KycVerified,
    int KycLevel,
    string ReferralCode);

public record SwiftyExWallet(
    string WalletType,
    string Blockchain,
    decimal Balance,
    string DepositAddress);

public record SwiftyExTransaction(
    string Id,
    string Type,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt);

public record SwiftyExRate(
    string Asset,
    decimal BuyRate,
    decimal SellRate);