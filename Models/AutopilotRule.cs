using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SwiftyAutopilot.Models.Enums;

namespace SwiftyAutopilot.Models;

public class AutopilotRule
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public long UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    [System.Text.Json.Serialization.JsonIgnore]
    public AppUser User { get; set; } = null!;

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    public TriggerType TriggerType { get; set; }
    public ActionType ActionType { get; set; }

    // ── Trigger Config ────────────────────────────────
    [MaxLength(20)]
    public string? TargetAsset { get; set; }       // e.g. "USDT", "BTC"
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? TargetPrice { get; set; }      // e.g. 1850.00 (NGN)
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal? PriceDropPercent { get; set; } // e.g. 5.00 (5%)
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? BalanceThreshold { get; set; } // e.g. 200000.00 (NGN)
    
    [MaxLength(50)]
    public string? CronExpression { get; set; }    // e.g. "0 18 * * 5"

    // ── Action Config ─────────────────────────────────
    [MaxLength(20)]
    public string? ActionAsset { get; set; }       // e.g. "BTC", "USDT"
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ActionAmount { get; set; }     // e.g. 200.00
    
    [MaxLength(20)]
    public string? ActionCurrency { get; set; }    // e.g. "USDT", "NGN"
    
    [MaxLength(50)]
    public string? BankAccount { get; set; }       // for withdrawals
    
    [MaxLength(30)]
    public string? BillType { get; set; }          // "DSTV","AIRTIME","ELECTRICITY"

    // ── Meta ──────────────────────────────────────────
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTriggeredAt { get; set; }
    public int TimesTriggered { get; set; } = 0;

    public List<ExecutionLog> Logs { get; set; } = new();
}
