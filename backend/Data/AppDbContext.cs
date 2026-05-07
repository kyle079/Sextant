using Microsoft.EntityFrameworkCore;
using Sextant.Models;

namespace Sextant.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CharacterToken> CharacterTokens => Set<CharacterToken>();
    public DbSet<WormholeSystem> WormholeSystems => Set<WormholeSystem>();
    public DbSet<WormholeType> WormholeTypes => Set<WormholeType>();
    public DbSet<SdeRefreshRun> SdeRefreshRuns => Set<SdeRefreshRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Timestamp).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.Characters).WithOne(x => x.User).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<CharacterToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CharacterId).IsUnique();
        });

        modelBuilder.Entity<WormholeSystem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SolarSystemId).IsUnique();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.UpdatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<WormholeType>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.UpdatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<SdeRefreshRun>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StartedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            e.Property(x => x.CompletedAt).HasConversion(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
        });
    }
}
