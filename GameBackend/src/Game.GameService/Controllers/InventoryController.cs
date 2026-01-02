using Game.GameService.Models;
using Game.GameService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Game.GameService.Controllers;

[ApiController]
[Route("api/player/{characterId}/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public ActionResult<InventoryResponse> GetInventory(int characterId, [FromQuery] string scope = "Global")
    {
        var inventory = _inventoryService.GetInventory(characterId, scope);
        return Ok(inventory);
    }

    [HttpPost("add")]
    public ActionResult AddItem(int characterId, [FromBody] AddItemRequest request)
    {
        var success = _inventoryService.AddItem(characterId, request);
        if (success)
        {
            return Ok();
        }
        return BadRequest("Failed to add item");
    }
    
    // 预留：移除物品接口
    [HttpPost("remove")]
    public ActionResult RemoveItem(int characterId, [FromQuery] string itemId, [FromQuery] int amount, [FromQuery] string scope = "Global")
    {
        var success = _inventoryService.RemoveItem(characterId, itemId, amount, scope);
        if (success)
        {
            return Ok();
        }
        return BadRequest("Failed to remove item (not found or insufficient amount)");
    }
}
