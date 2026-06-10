using SwiftyAutopilot.Models;

namespace SwiftyAutopilot.Repositories.Interfaces;

public interface IUserRepository
{
    // Get user by Telegram ID
    Task<AppUser?> GetByTelegramIdAsync(long telegramId);
    
    // Get all users
    Task<IEnumerable<AppUser>> GetAllAsync();
    
    // Create new user
    Task<AppUser> CreateAsync(AppUser user);
    
    // Update existing user
    Task<AppUser> UpdateAsync(AppUser user);
    
    // Soft delete user
    Task<bool> DeactivateAsync(long telegramId);
    
    // Check if user exists
    Task<bool> ExistsAsync(long telegramId);
}