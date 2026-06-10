using SwiftyAutopilot.Models;
using SwiftyAutopilot.Repositories.Interfaces;
using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Services;

public class StreakService(
    IUserRepository userRepository, ITelegramService telegramService) : IStreakService

{
    // ── Badge definitions ─────────────────────────────
    private static readonly Dictionary<string, Func<AppUser, bool>>
        BadgeConditions = new()
    {
        { "🌱 First Step",      u => u.TotalDaysOpened >= 1    },
        { "🔥 Week Warrior",    u => u.CurrentStreak  >= 7     },
        { "💪 Two Weeks",       u => u.CurrentStreak  >= 14    },
        { "🏆 Month Master",    u => u.CurrentStreak  >= 30    },
        { "👑 100 Day Legend",  u => u.CurrentStreak  >= 100   },
        { "⚡ Rule Creator",    u => u.Rules.Count    >= 1     },
        { "🤖 AI Explorer",     u => u.Rules.Count    >= 3     },
        { "🚀 Autopilot Pro",   u => u.Rules.Count    >= 10    },
        { "💰 Stack Master",    u => u.TotalDaysOpened >= 50   },
    };

    public async Task<StreakResult> RecordOpenAsync(
        long telegramId)
    {
        var user = await userRepository
            .GetByTelegramIdAsync(telegramId);

        if (user is null)
            return EmptyResult();

        var now      = DateTime.UtcNow.Date;
        var lastOpen = user.LastOpenedAt?.Date;
        var isNewDay = lastOpen != now;
        var streakBroken = false;
        var newBadges    = new List<string>();

        if (isNewDay)
        {
            // Check if streak is broken
            // (missed more than 1 day)
            if (lastOpen.HasValue &&
                (now - lastOpen.Value).TotalDays > 1)
            {
                // Streak broken 💔
                user.CurrentStreak = 1;
                streakBroken       = true;
            }
            else
            {
                // Streak continues 🔥
                user.CurrentStreak++;
            }

            // Update longest streak
            if (user.CurrentStreak > user.LongestStreak)
                user.LongestStreak = user.CurrentStreak;

            user.TotalDaysOpened++;
            user.LastOpenedAt = DateTime.UtcNow;

            // Check for new badges
            newBadges = await CheckBadgesAsync(user);

            await userRepository.UpdateAsync(user);
        }
        if (newBadges.Count > 0)
        {
            foreach (var badge in newBadges)
            {
                await telegramService.SendMessageAsync(
                    telegramId,
                    $"🎉 <b>New Badge Unlocked!</b>\n\n" +
                    $"{badge}\n\n" +
                    $"<i>Keep your streak going!</i> 🔥");
            }
        }

        return BuildResult(user, isNewDay,
            streakBroken, newBadges);
    }

    public async Task<StreakResult> GetStreakAsync(
        long telegramId)
    {
        var user = await userRepository
            .GetByTelegramIdAsync(telegramId);

        if (user is null) return EmptyResult();

        return BuildResult(user, false, false,
            new List<string>());
    }

    public Task<List<string>> CheckBadgesAsync(AppUser user)
    {
        var newBadges = new List<string>();

        foreach (var (badge, condition) in BadgeConditions)
        {
            if (!user.Badges.Contains(badge)
                && condition(user))
            {
                user.Badges.Add(badge)   ;
                newBadges.Add(badge);
            }
        }

        return Task.FromResult(newBadges);
    }

    // ── Helpers ───────────────────────────────────────
    private static StreakResult BuildResult(
        AppUser      user,
        bool         isNewDay,
        bool         streakBroken,
        List<string> newBadges)
    {
        var streak  = user.CurrentStreak;
        var fire    = GetFireEmoji(streak);
        var message = GetStreakMessage(
            streak, isNewDay, streakBroken);

        return new StreakResult(
            CurrentStreak: streak,
            LongestStreak: user.LongestStreak,
            TotalDays:     user.TotalDaysOpened,
            IsNewDay:      isNewDay,
            StreakBroken:  streakBroken,
            FireEmoji:     fire,
            Message:       message,
            Badges:        user.Badges,
            NewBadges:     newBadges);
    }

    private static string GetFireEmoji(int streak) =>
        streak switch
        {
            0      => "💤",
            >= 100 => "👑🔥🔥🔥🔥",
            >= 30  => "🏆🔥🔥🔥",
            >= 14  => "🔥🔥🔥",
            >= 7   => "🔥🔥",
            1      => "🔥",
            _      => "🔥"
        };
    private static string GetStreakMessage(
        int streak, bool isNewDay, bool broken) =>
        broken ? "Streak reset. Start again! 💪" :
        !isNewDay ? $"{streak} day streak — keep it up!" :
        streak switch
        {
            1   => "Streak started! Come back tomorrow 🔥",
            7   => "7 days! You're on fire! 🔥🔥",
            14  => "2 weeks strong! Legendary! 🔥🔥🔥",
            30  => "30 DAYS! You're a master! 🏆",
            100 => "100 DAYS! ABSOLUTE LEGEND! 👑",
            _   => $"{streak} day streak! Keep going! 🔥"
        };

    private static StreakResult EmptyResult() =>
        new(0, 0, 0, false, false,
            "💤", "Start your streak!", 
            new List<string>(), new List<string>());
}