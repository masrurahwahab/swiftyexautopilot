using SwiftyAutopilot.Models;

namespace SwiftyAutopilot.Services.Interfaces;

public interface IUserService
{
    // Get user by Telegram ID
    Task<AppUser?> GetUserAsync(long telegramId);
    
    // Register new user or return existing
    Task<AppUser> RegisterOrLoginAsync(long telegramId, 
        string firstName, string? username);
    
    // Update user info
    Task<AppUser> UpdateUserAsync(long telegramId, 
        string firstName, string? username);
    
    // Deactivate user account
    Task<bool> DeactivateUserAsync(long telegramId);
    
    // Check if user exists
    Task<bool> UserExistsAsync(long telegramId);
}