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
    public DbSet<ChainSystem> ChainSystems => Set<ChainSystem>();
    public DbSet<ChainConnection> ChainConnections => Set<ChainConnection>();
    public DbSet<Signature> Signatures => Set<Signature>();

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

        modelBuilder.Entity<ChainSystem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SolarSystemId);
            e.HasQueryFilter(x => x.DeletedAt == null);
            e.HasMany(x => x.SourceConnections).WithOne(x => x.SourceSystem)
                .HasForeignKey(x => x.SourceSystemId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.TargetConnections).WithOne(x => x.TargetSystem)
                .HasForeignKey(x => x.TargetSystemId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Signatures).WithOne(x => x.ChainSystem)
                .HasForeignKey(x => x.ChainSystemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChainConnection>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.DeletedAt == null);
            e.HasIndex(x => new { x.SourceSystemId, x.TargetSystemId });
        });

        modelBuilder.Entity<Signature>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => x.DeletedAt == null);
            e.HasIndex(x => new { x.ChainSystemId, x.SignatureId }).IsUnique();
        });

    }
}
