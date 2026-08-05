using Microsoft.EntityFrameworkCore;
using PancakeBot.Api.model.player;

namespace PancakeBot.Api.data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(player => player.AccountId);

            entity.Property(player => player.AccountId)
                .IsRequired();

            entity.Property(player => player.DisplayName)
                .IsRequired();

            entity.Property(player => player.CreatedAtUtc)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
    
    public DbSet<Player> Players => Set<Player>();
}
