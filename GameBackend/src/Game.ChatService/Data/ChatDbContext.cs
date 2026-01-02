using Game.ChatService.Models;
using Microsoft.EntityFrameworkCore;

namespace Game.ChatService.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<ChatMessage> Messages { get; set; }
    public DbSet<ChatRoom> Rooms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChatMessage>()
            .HasIndex(m => m.RoomId);
            
        modelBuilder.Entity<ChatMessage>()
            .HasIndex(m => m.Timestamp);

        // Seed default rooms
        modelBuilder.Entity<ChatRoom>().HasData(
            new ChatRoom
            {
                Id = 1,
                Name = "Global",
                Type = "Public",
                IsPersistent = true
            },
            new ChatRoom
            {
                Id = 2,
                Name = "Trade",
                Type = "Public",
                IsPersistent = true
            }
        );
    }
}