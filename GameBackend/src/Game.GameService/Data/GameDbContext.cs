using Microsoft.EntityFrameworkCore;
using Game.GameService.Models;

namespace Game.GameService.Data;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    public DbSet<PlayerInventoryItem> InventoryItems { get; set; }
    public DbSet<PlayerCharacter> Characters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlayerCharacter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<PlayerInventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ItemId).IsRequired();
            entity.Property(e => e.Scope).HasDefaultValue("Global");
            
            // Index for faster lookups by character and scope
            entity.HasIndex(e => e.CharacterId);
            entity.HasIndex(e => new { e.CharacterId, e.Scope });
        });
    }
}
