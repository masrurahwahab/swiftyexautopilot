using Microsoft.EntityFrameworkCore;
using SwiftyAutopilot.Data;
using SwiftyAutopilot.Models;
using SwiftyAutopilot.Repositories.Interfaces;

namespace SwiftyAutopilot.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<AppUser?> GetByTelegramIdAsync(long telegramId)
    {
        return await db.Users
            .Include(u => u.Rules)
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
    }

    public async Task<IEnumerable<AppUser>> GetAllAsync()
    {
        return await db.Users
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<AppUser> CreateAsync(AppUser user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<AppUser> UpdateAsync(AppUser user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeactivateAsync(long telegramId)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
        
        if (user is null) return false;

        user.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(long telegramId)
    {
        return await db.Users
            .AnyAsync(u => u.TelegramId == telegramId);
    }
}