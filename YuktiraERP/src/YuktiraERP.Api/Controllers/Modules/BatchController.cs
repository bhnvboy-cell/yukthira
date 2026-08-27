using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Caching;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/v1/mm/batch")]
[Authorize]
public class BatchController : ControllerBase
{
    private readonly IBatchService _batchService;
    private readonly ITenantContext _tenant;

    public BatchController(IBatchService batchService, ITenantContext tenant)
    {
        _batchService = batchService;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var batches = await _batchService.GetAllBatchesAsync();
        return Ok(new { data = batches, tenantId = _tenant.TenantId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var batch = await _batchService.GetBatchAsync(id);
        return batch == null ? NotFound() : Ok(new { data = batch, tenantId = _tenant.TenantId });
    }

    [HttpGet("by-number/{batchNumber}")]
    public async Task<IActionResult> GetByNumber(string batchNumber)
    {
        var batch = await _batchService.GetBatchByNumberAsync(batchNumber);
        return batch == null ? NotFound() : Ok(new { data = batch, tenantId = _tenant.TenantId });
    }

    [HttpGet("by-material/{materialId:guid}")]
    public async Task<IActionResult> GetByMaterial(Guid materialId)
    {
        var batches = await _batchService.GetBatchesByMaterialAsync(materialId);
        return Ok(new { data = batches, tenantId = _tenant.TenantId });
    }

    [HttpPost]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Create([FromBody] BatchEntity batch)
    {
        batch.TenantId = _tenant.TenantId;
        var result = await _batchService.CreateBatchAsync(batch);
        return Ok(new { success = true, id = result.Id, tenantId = _tenant.TenantId });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Update(Guid id, [FromBody] BatchEntity batch)
    {
        var existing = await _batchService.GetBatchAsync(id);
        if (existing == null) return NotFound();
        batch.Id = id;
        batch.TenantId = existing.TenantId;
        var result = await _batchService.UpdateBatchAsync(batch);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpPost("{id:guid}/expire")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Expire(Guid id)
    {
        await _batchService.ExpireBatchAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpPost("recall")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Recall([FromBody] RecallRequest request)
    {
        var recall = await _batchService.RecallBatchAsync(
            request.RecallNumber, request.BatchIds, request.Reason, request.InitiatedBy);
        return Ok(new { success = true, data = recall, tenantId = _tenant.TenantId });
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var history = await _batchService.GetBatchHistoryAsync(id);
        return Ok(new { data = history, tenantId = _tenant.TenantId });
    }

    [HttpGet("{id:guid}/traceability")]
    public async Task<IActionResult> GetTraceability(Guid id)
    {
        var trace = await _batchService.GetBatchTraceabilityAsync(id);
        return Ok(new { data = trace, tenantId = _tenant.TenantId });
    }

    [HttpPost("movement")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> RecordMovement([FromBody] BatchMovementEntity movement)
    {
        movement.TenantId = _tenant.TenantId;
        var result = await _batchService.RecordMovementAsync(movement);
        return Ok(new { success = true, id = result.Id, tenantId = _tenant.TenantId });
    }

    [HttpPost("check-expiry")]
    public async Task<IActionResult> CheckExpiry()
    {
        var expired = await _batchService.CheckExpiryAsync();
        return Ok(new { data = expired, count = expired.Count, tenantId = _tenant.TenantId });
    }

    [HttpGet("{id:guid}/certificate")]
    public async Task<IActionResult> GetCertificate(Guid id)
    {
        var cert = await _batchService.GenerateBatchCertificateAsync(id);
        return Ok(new { certificate = cert, tenantId = _tenant.TenantId });
    }

    // Serial Number endpoints
    [HttpGet("serial/{id:guid}")]
    public async Task<IActionResult> GetSerial(Guid id)
    {
        var serial = await _batchService.GetSerialNumberAsync(id);
        return serial == null ? NotFound() : Ok(new { data = serial, tenantId = _tenant.TenantId });
    }

    [HttpGet("serial/by-number/{serialNumber}")]
    public async Task<IActionResult> GetSerialByNumber(string serialNumber)
    {
        var serial = await _batchService.GetSerialNumberByNumberAsync(serialNumber);
        return serial == null ? NotFound() : Ok(new { data = serial, tenantId = _tenant.TenantId });
    }

    [HttpGet("serial/by-batch/{batchId:guid}")]
    public async Task<IActionResult> GetSerialsByBatch(Guid batchId)
    {
        var serials = await _batchService.GetSerialNumbersByBatchAsync(batchId);
        return Ok(new { data = serials, tenantId = _tenant.TenantId });
    }

    [HttpPost("serial")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateSerial([FromBody] SerialNumberEntity serial)
    {
        serial.TenantId = _tenant.TenantId;
        var result = await _batchService.CreateSerialNumberAsync(serial);
        return Ok(new { success = true, id = result.Id, tenantId = _tenant.TenantId });
    }

    [HttpGet("serial/history/{serialNumber}")]
    public async Task<IActionResult> GetSerialHistory(string serialNumber)
    {
        var history = await _batchService.GetSerialHistoryAsync(serialNumber);
        return Ok(new { data = history, tenantId = _tenant.TenantId });
    }

    // Recall endpoints
    [HttpGet("recalls")]
    public async Task<IActionResult> GetRecalls()
    {
        var recalls = await _batchService.GetRecallsAsync();
        return Ok(new { data = recalls, tenantId = _tenant.TenantId });
    }

    [HttpPut("recalls/{id:guid}/status")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateRecallStatus(Guid id, [FromBody] UpdateRecallStatusRequest request)
    {
        var recall = await _batchService.UpdateRecallStatusAsync(id, request.Status, request.ResolutionNotes);
        return recall == null ? NotFound() : Ok(new { success = true, data = recall, tenantId = _tenant.TenantId });
    }
}

public class RecallRequest
{
    public string RecallNumber { get; set; } = "";
    public List<Guid> BatchIds { get; set; } = new();
    public string Reason { get; set; } = "";
    public Guid InitiatedBy { get; set; }
}

public class UpdateRecallStatusRequest
{
    public string Status { get; set; } = "";
    public string ResolutionNotes { get; set; } = "";
}
