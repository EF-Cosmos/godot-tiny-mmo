using Game.GameService.Data;
using Game.GameService.Models;
using Microsoft.EntityFrameworkCore;

namespace Game.GameService.Services;

public class DbInventoryService : IInventoryService
{
    private readonly GameDbContext _context;
    private readonly ILogger<DbInventoryService> _logger;

    public DbInventoryService(GameDbContext context, ILogger<DbInventoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public InventoryResponse GetInventory(int characterId, string scope)
    {
        var items = _context.InventoryItems
            .Where(i => i.CharacterId == characterId && (i.Scope == "Global" || i.Scope == scope))
            .ToList();

        return new InventoryResponse
        {
            CharacterId = characterId,
            Items = items
        };
    }

    public bool AddItem(int characterId, AddItemRequest request)
    {
        try
        {
            // Check if item already exists (stackable)
            var existingItem = _context.InventoryItems
                .FirstOrDefault(i => i.CharacterId == characterId && 
                                     i.ItemId == request.ItemId && 
                                     i.Scope == request.Scope);

            if (existingItem != null)
            {
                existingItem.Amount += request.Amount;
            }
            else
            {
                // Find first empty slot (simplified logic, just append for now)
                // In a real game, you'd check for max slots and find gaps
                int nextSlot = 0;
                if (_context.InventoryItems.Any(i => i.CharacterId == characterId))
                {
                    nextSlot = _context.InventoryItems
                        .Where(i => i.CharacterId == characterId)
                        .Max(i => i.SlotIndex) + 1;
                }

                var newItem = new PlayerInventoryItem
                {
                    CharacterId = characterId,
                    ItemId = request.ItemId,
                    Amount = request.Amount,
                    Scope = request.Scope,
                    SlotIndex = nextSlot
                };
                _context.InventoryItems.Add(newItem);
            }

            _context.SaveChanges();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item for character {CharacterId}", characterId);
            return false;
        }
    }

    public bool RemoveItem(int characterId, string itemId, int amount, string scope)
    {
        try
        {
            var item = _context.InventoryItems
                .FirstOrDefault(i => i.CharacterId == characterId && 
                                     i.ItemId == itemId && 
                                     i.Scope == scope);

            if (item == null || item.Amount < amount)
            {
                return false;
            }

            item.Amount -= amount;
            if (item.Amount <= 0)
            {
                _context.InventoryItems.Remove(item);
            }

            _context.SaveChanges();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item for character {CharacterId}", characterId);
            return false;
        }
    }
}
