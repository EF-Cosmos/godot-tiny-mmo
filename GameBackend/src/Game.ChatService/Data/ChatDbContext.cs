using Game.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Game.ChatService.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Room> Rooms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Avatar).HasMaxLength(500);
        });

        // Room configuration
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Settings).HasColumnType("jsonb");
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.SenderUsername).HasMaxLength(50);
            entity.Property(e => e.RecipientId).HasMaxLength(50);
            entity.HasIndex(e => e.RoomId);
            entity.HasIndex(e => e.SenderId);
            entity.HasIndex(e => e.Timestamp);
        });

        // Seed default rooms
        modelBuilder.Entity<Room>().HasData(
            new Room
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "Global",
                Description = "Global chat channel",
                Type = RoomType.Public,
                Status = RoomStatus.InProgress,
                MaxPlayers = 1000,
                CurrentPlayers = 0,
                CreatedAt = DateTime.UtcNow,
                Settings = new Dictionary<string, string>()
            },
            new Room
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "Trade",
                Description = "Trading chat channel",
                Type = RoomType.Public,
                Status = RoomStatus.InProgress,
                MaxPlayers = 1000,
                CurrentPlayers = 0,
                CreatedAt = DateTime.UtcNow,
                Settings = new Dictionary<string, string>()
            }
        );
    }
}