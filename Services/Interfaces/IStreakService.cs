using SwiftyAutopilot.Models;

namespace SwiftyAutopilot.Services.Interfaces;

public interface IStreakService
{
    // Call when user opens the app
    Task<StreakResult> RecordOpenAsync(long telegramId);

    // Get current streak info
    Task<StreakResult> GetStreakAsync(long telegramId);

    // Check and award badges
    Task<List<string>> CheckBadgesAsync(AppUser user);
}

public record StreakResult(
    int     CurrentStreak,
    int     LongestStreak,
    int     TotalDays,
    bool    IsNewDay,
    bool    StreakBroken,
    string  FireEmoji,
    string  Message,
    List<string> Badges,
    List<string> NewBadges);