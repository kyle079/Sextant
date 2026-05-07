using Microsoft.EntityFrameworkCore;
using Sextant.Models;

namespace Sextant.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Timestamp).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });
    }
}
