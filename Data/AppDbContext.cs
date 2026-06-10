

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SwiftyAutopilot.Models;

namespace SwiftyAutopilot.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) 
    : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AutopilotRule> Rules => Set<AutopilotRule>();
    public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── AppUser ───────────────────────────────────
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(u => u.TelegramId);
            
            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Username)
                .HasMaxLength(100);

            entity.HasMany(u => u.Rules)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── AutopilotRule ─────────────────────────────
        modelBuilder.Entity<AutopilotRule>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Id)
                .ValueGeneratedOnAdd();

            entity.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(r => r.TargetPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(r => r.PriceDropPercent)
                .HasColumnType("decimal(5,2)");

            entity.Property(r => r.BalanceThreshold)
                .HasColumnType("decimal(18,2)");

            entity.Property(r => r.ActionAmount)
                .HasColumnType("decimal(18,2)");

            entity.HasMany(r => r.Logs)
                .WithOne(l => l.Rule)
                .HasForeignKey(l => l.RuleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for faster rule engine queries
            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => r.IsActive);
            entity.HasIndex(r => r.TriggerType);
        });

        // ── ExecutionLog ──────────────────────────────
        modelBuilder.Entity<ExecutionLog>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.Property(l => l.Id)
                .ValueGeneratedOnAdd();

            entity.Property(l => l.Message)
                .HasMaxLength(500);

            entity.Property(l => l.AmountProcessed)
                .HasColumnType("decimal(18,2)");

            // Index for faster log queries per rule
            entity.HasIndex(l => l.RuleId);
            entity.HasIndex(l => l.ExecutedAt);
        });
        

        var comparer = new ValueComparer<List<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        modelBuilder.Entity<AppUser>()
            .Property(u => u.Badges)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(
                    v,
                    (System.Text.Json.JsonSerializerOptions?)null),

                v => string.IsNullOrWhiteSpace(v)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                          v,
                          (System.Text.Json.JsonSerializerOptions?)null)
                      ?? new List<string>());
    }
}