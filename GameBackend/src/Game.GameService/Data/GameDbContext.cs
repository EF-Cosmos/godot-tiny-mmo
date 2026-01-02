using Microsoft.EntityFrameworkCore;
using Game.GameService.Models;

namespace Game.GameService.Data;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    public DbSet<PlayerInventoryItem> InventoryItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlayerInventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ItemId).IsRequired();
            entity.Property(e => e.Scope).HasDefaultValue("Global");
            
            // Index for faster lookups by character and scope
            entity.HasIndex(e => e.CharacterId);
            entity.HasIndex(e => e.CharacterId, e.Scope);
        });
    }
}
