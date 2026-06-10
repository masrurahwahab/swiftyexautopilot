using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftyAutopilot.Models;

public class AppUser
{
    [Key]
    public long TelegramId { get; set; }
    
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = "";
    
    [MaxLength(100)]
    public string? Username { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    [System.Text.Json.Serialization.JsonIgnore]
    public List<AutopilotRule> Rules { get; set; } = new();
    
    public int CurrentStreak    { get; set; } = 0;
    public int LongestStreak    { get; set; } = 0;
    public DateTime? LastOpenedAt { get; set; }
    public int TotalDaysOpened  { get; set; } = 0;
    public List<string> Badges  { get; set; } = new();
}
