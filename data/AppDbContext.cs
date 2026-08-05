using Microsoft.EntityFrameworkCore;
using PancakeBot.Api.model.player;
using PancakeBot.Api.model.totd;

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

        modelBuilder.Entity<TotdMap>(entity =>
        {
            entity.HasKey(totdMap => totdMap.MapUid);

            entity.Property(totdMap => totdMap.MapUid)
                .IsRequired();

            entity.Property(totdMap => totdMap.TotdDate)
                .HasColumnType("date");
        });
    }
    
    public DbSet<Player> Players => Set<Player>();
    public DbSet<TotdMap> TotdMaps => Set<TotdMap>();
}
