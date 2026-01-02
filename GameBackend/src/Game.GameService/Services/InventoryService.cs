using System.Collections.Concurrent;
using Game.GameService.Models;

namespace Game.GameService.Services;

public interface IInventoryService
{
    InventoryResponse GetInventory(int characterId, string scope);
    bool AddItem(int characterId, AddItemRequest request);
    bool RemoveItem(int characterId, string itemId, int amount, string scope);
}

public class InMemoryInventoryService : IInventoryService
{
    // Key: CharacterId, Value: List of Items
    // 在生产环境中，这里应该替换为 Entity Framework Core (Postgres)
    private readonly ConcurrentDictionary<int, List<PlayerInventoryItem>> _inventoryStore = new();
    private readonly ILogger<InMemoryInventoryService> _logger;
    private int _nextId = 1;

    public InMemoryInventoryService(ILogger<InMemoryInventoryService> logger)
    {
        _logger = logger;
        // 初始化一些测试数据
        _inventoryStore[1] = new List<PlayerInventoryItem>
        {
            new() { Id = _nextId++, CharacterId = 1, ItemId = "sword_01", Amount = 1, SlotIndex = 0, Scope = "Global" },
            new() { Id = _nextId++, CharacterId = 1, ItemId = "potion_hp", Amount = 5, SlotIndex = 1, Scope = "Global" }
        };
    }

    public InventoryResponse GetInventory(int characterId, string scope)
    {
        var items = _inventoryStore.GetOrAdd(characterId, _ => new List<PlayerInventoryItem>());
        
        // 返回全局物品 + 当前作用域的物品
        var filteredItems = items
            .Where(i => i.Scope == "Global" || i.Scope == scope)
            .ToList();

        return new InventoryResponse
        {
            CharacterId = characterId,
            Items = filteredItems
        };
    }

    public bool AddItem(int characterId, AddItemRequest request)
    {
        var items = _inventoryStore.GetOrAdd(characterId, _ => new List<PlayerInventoryItem>());
        
        // 查找是否已存在可堆叠的物品
        var existingItem = items.FirstOrDefault(i => i.ItemId == request.ItemId && i.Scope == request.Scope);
        
        if (existingItem != null)
        {
            existingItem.Amount += request.Amount;
        }
        else
        {
            // 简单的寻找空位逻辑
            int slot = 0;
            while (items.Any(i => i.SlotIndex == slot)) slot++;

            items.Add(new PlayerInventoryItem
            {
                Id = _nextId++,
                CharacterId = characterId,
                ItemId = request.ItemId,
                Amount = request.Amount,
                SlotIndex = slot,
                Scope = request.Scope
            });
        }
        
        _logger.LogInformation("Added {Amount} x {ItemId} to Char {CharId} (Scope: {Scope})", 
            request.Amount, request.ItemId, characterId, request.Scope);
            
        return true;
    }

    public bool RemoveItem(int characterId, string itemId, int amount, string scope)
    {
        if (!_inventoryStore.TryGetValue(characterId, out var items)) return false;

        var item = items.FirstOrDefault(i => i.ItemId == itemId && (i.Scope == scope || i.Scope == "Global"));
        
        if (item == null || item.Amount < amount) return false;

        item.Amount -= amount;
        if (item.Amount <= 0)
        {
            items.Remove(item);
        }
        
        return true;
    }
}
