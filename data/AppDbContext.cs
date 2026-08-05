using Microsoft.EntityFrameworkCore;
using PancakeBot.Api.model.player;

namespace PancakeBot.Api.data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Player> Players => Set<Player>();
}