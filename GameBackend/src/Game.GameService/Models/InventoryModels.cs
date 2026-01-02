namespace Game.GameService.Models;

public class PlayerInventoryItem
{
    public int Id { get; set; }
    public int CharacterId { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public int SlotIndex { get; set; } // 背包格子位置
    
    // 作用域：Global (全局), World_1 (特定世界), Instance_X (副本)
    public string Scope { get; set; } = "Global"; 
    
    // 物品元数据（比如耐久度、附魔等，存成 JSON）
    public string Metadata { get; set; } = "{}";
}

public class AddItemRequest
{
    public string ItemId { get; set; } = string.Empty;
    public int Amount { get; set; } = 1;
    public string Scope { get; set; } = "Global";
}

public class InventoryResponse
{
    public int CharacterId { get; set; }
    public List<PlayerInventoryItem> Items { get; set; } = new();
}
