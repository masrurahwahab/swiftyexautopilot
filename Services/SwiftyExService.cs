using System.Text;
using System.Text.Json;
using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Services;

public class SwiftyExService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : ISwiftyExService
{
    // Base URL from appsettings
    // e.g. "http://localhost:8000"
    private string BaseUrl => 
        configuration["SwiftyEx:BaseUrl"] 
        ?? "http://localhost:8000";

    private HttpClient Client => 
        httpClientFactory.CreateClient("SwiftyEx");

    public async Task<SwiftyExUser?> GetUserProfileAsync(
        string initData)
    {
        var body    = BuildBody(new { initData });
        var response = await Client.PostAsync(
            $"{BaseUrl}/miniapp/me", body);

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return Deserialize<SwiftyExUser>(json);
    }

    public async Task<List<SwiftyExWallet>> GetWalletsAsync(
        string initData)
    {
        var body     = BuildBody(new { initData });
        var response = await Client.PostAsync(
            $"{BaseUrl}/miniapp/wallets", body);

        if (!response.IsSuccessStatusCode) 
            return new List<SwiftyExWallet>();

        var json = await response.Content.ReadAsStringAsync();
        return Deserialize<List<SwiftyExWallet>>(json) 
               ?? new List<SwiftyExWallet>();
    }

    public async Task<List<SwiftyExTransaction>> GetTransactionsAsync(
        string initData,
        int page = 1,
        string? walletType = null)
    {
        var body = BuildBody(new
        {
            initData,
            page,
            wallet_type = walletType ?? ""
        });

        var response = await Client.PostAsync(
            $"{BaseUrl}/miniapp/transactions", body);

        if (!response.IsSuccessStatusCode) 
            return new List<SwiftyExTransaction>();

        var json = await response.Content.ReadAsStringAsync();
        return Deserialize<List<SwiftyExTransaction>>(json) 
               ?? new List<SwiftyExTransaction>();
    }

    public async Task<List<SwiftyExRate>> GetRatesAsync()
    {
        // Public endpoint — no initData needed
        var response = await Client.GetAsync(
            $"{BaseUrl}/miniapp/rates");

        if (!response.IsSuccessStatusCode) 
            return new List<SwiftyExRate>();

        var json = await response.Content.ReadAsStringAsync();
        return Deserialize<List<SwiftyExRate>>(json) 
               ?? new List<SwiftyExRate>();
    }

    // ── Private Helpers ───────────────────────────────
    private static StringContent BuildBody(object payload)
    {
        var json = JsonSerializer.Serialize(payload,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

        return new StringContent(
            json,
            Encoding.UTF8,
            "application/json");
    }

    private static T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch
        {
            return default;
        }
    }
}