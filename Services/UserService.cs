using SwiftyAutopilot.Models;
using SwiftyAutopilot.Repositories.Interfaces;
using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Services;

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<AppUser?> GetUserAsync(long telegramId)
    {
        return await userRepository
            .GetByTelegramIdAsync(telegramId);
    }

    public async Task<AppUser> RegisterOrLoginAsync(
        long telegramId,
        string firstName,
        string? username)
    {
        // Return existing user if found
        var existing = await userRepository
            .GetByTelegramIdAsync(telegramId);

        if (existing is not null)
        {
            // Refresh name in case it changed in Telegram
            existing.FirstName = firstName;
            existing.Username  = username;
            return await userRepository.UpdateAsync(existing);
        }

        // First time — create new user
        var newUser = new AppUser
        {
            TelegramId = telegramId,
            FirstName  = firstName,
            Username   = username,
            CreatedAt  = DateTime.UtcNow,
            IsActive   = true
        };

        return await userRepository.CreateAsync(newUser);
    }

    public async Task<AppUser> UpdateUserAsync(
        long telegramId,
        string firstName,
        string? username)
    {
        var user = await userRepository
            .GetByTelegramIdAsync(telegramId)
            ?? throw new KeyNotFoundException(
                $"User {telegramId} not found.");

        user.FirstName = firstName;
        user.Username  = username;

        return await userRepository.UpdateAsync(user);
    }

    public async Task<bool> DeactivateUserAsync(long telegramId)
    {
        var exists = await userRepository
            .ExistsAsync(telegramId);

        if (!exists) return false;

        return await userRepository.DeactivateAsync(telegramId);
    }

    public async Task<bool> UserExistsAsync(long telegramId)
    {
        return await userRepository.ExistsAsync(telegramId);
    }
}