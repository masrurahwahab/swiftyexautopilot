using Microsoft.AspNetCore.Mvc;
using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SwiftyExController(
    ISwiftyExService swiftyExService) : ControllerBase
{
    // GET api/swiftyex/rates
    // Public — no auth needed
    [HttpGet("rates")]
    public async Task<IActionResult> GetRates()
    {
        var rates = await swiftyExService.GetRatesAsync();
        return Ok(rates);
    }

    // GET api/swiftyex/wallets
    [HttpGet("wallets")]
    public async Task<IActionResult> GetWallets()
    {
        var initData = GetInitData();
        var wallets  = await swiftyExService
            .GetWalletsAsync(initData);

        return Ok(wallets);
    }

    // GET api/swiftyex/transactions
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] string? walletType = null)
    {
        var initData     = GetInitData();
        var transactions = await swiftyExService
            .GetTransactionsAsync(initData, page, walletType);

        return Ok(transactions);
    }

    // ── Private Helpers ───────────────────────────────
    private string GetInitData()
    {
        // Passed from frontend Telegram Mini App
        return HttpContext.Request.Headers["X-Init-Data"]
            .FirstOrDefault() ?? "";
    }
}