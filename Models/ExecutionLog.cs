
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SwiftyAutopilot.Models;

public class ExecutionLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid RuleId { get; set; }
    
    [ForeignKey(nameof(RuleId))]
    [System.Text.Json.Serialization.JsonIgnore]
    public AutopilotRule Rule { get; set; } = null!;

    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    
    [MaxLength(500)]
    public string Message { get; set; } = "";
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? AmountProcessed { get; set; }
}
