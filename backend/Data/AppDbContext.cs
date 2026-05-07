using Microsoft.EntityFrameworkCore;
using Sextant.Models;

namespace Sextant.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CharacterToken> CharacterTokens => Set<CharacterToken>();

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
    }
}
