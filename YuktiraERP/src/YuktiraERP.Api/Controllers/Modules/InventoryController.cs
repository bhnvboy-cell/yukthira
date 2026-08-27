using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/v1/mm/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("{materialId:guid}/availability")]
    public async Task<IActionResult> CheckAvailability(Guid materialId, [FromQuery] decimal quantity, [FromQuery] DateTime deliveryDate)
    {
        var result = await _inventoryService.CheckAvailabilityAsync(materialId, quantity, deliveryDate);
        return Ok(result);
    }

    [HttpPost("{materialId:guid}/reserve")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> ReserveStock(Guid materialId, [FromBody] ReserveStockRequest request)
    {
        var result = await _inventoryService.ReserveStockAsync(materialId, request.Quantity, request.OrderId);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("reservation/{reservationId:guid}")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> ReleaseReservation(Guid reservationId)
    {
        await _inventoryService.ReleaseReservationAsync(reservationId);
        return Ok(new { success = true });
    }

    [HttpGet("{materialId:guid}/confirmed-availability")]
    public async Task<IActionResult> GetConfirmedAvailability(Guid materialId, [FromQuery] string fromStore)
    {
        var result = await _inventoryService.GetConfirmedAvailabilityAsync(materialId, fromStore);
        return Ok(result);
    }
}

public class ReserveStockRequest
{
    public decimal Quantity { get; set; }
    public Guid OrderId { get; set; }
}
