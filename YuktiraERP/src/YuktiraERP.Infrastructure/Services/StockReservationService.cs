using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class StockReservationService : IStockReservationService
{
    private readonly YuktiraDbContext _db;

    public StockReservationService(YuktiraDbContext db) => _db = db;

    public async Task<ReservationResultDto> CreateReservationAsync(CreateReservationDto request, Guid tenantId, string userId)
    {
        var result = new ReservationResultDto();
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var resNumber = GenerateReservationNumber(tenantId);
            var now = DateTime.UtcNow;

            var header = new StockReservationHeaderEntity
            {
                TenantId = tenantId.ToString(),
                ReservationNumber = resNumber,
                RequirementDate = DateTime.TryParse(request.RequirementDate, out var rd) ? rd : now.AddDays(7),
                CostCenter = request.CostCenter,
                OrderNumber = request.OrderNumber,
                ProductionOrderNo = request.ProductionOrderNo,
                SalesOrderNo = request.SalesOrderNo,
                WBSElement = request.WBSElement,
                HeaderText = request.HeaderText,
                ReservationType = request.ReservationType.ToString(),
                Plant = request.Lines.FirstOrDefault()?.Plant ?? "1000",
                CreatedBy = userId,
                CreatedAt = now,
                Status = "Active"
            };
            _db.StockReservationHeaders.Add(header);
            await _db.SaveChangesAsync();

            var lineNum = 0;
            decimal totalReserved = 0;
            foreach (var line in request.Lines)
            {
                lineNum++;
                var available = await GetAvailableQuantityAsync(line.MaterialCode, line.Plant, line.StorageLocation, line.BatchNumber, tenantId);
                if (available < line.RequiredQuantity)
                {
                    result.Warnings.Add($"Insufficient stock for {line.MaterialCode}: Available {available}, Required {line.RequiredQuantity}");
                }

                var item = new StockReservationItemEntity
                {
                    TenantId = tenantId.ToString(),
                    ReservationHeaderId = header.Id.ToString(),
                    LineNumber = lineNum,
                    MaterialCode = line.MaterialCode,
                    MaterialName = line.MaterialName,
                    Plant = line.Plant,
                    StorageLocation = line.StorageLocation,
                    BatchNumber = line.BatchNumber,
                    RequiredQuantity = line.RequiredQuantity,
                    UOM = line.UOM,
                    ItemText = line.ItemText,
                    Status = "Active"
                };
                _db.StockReservationItems.Add(item);
                totalReserved += line.RequiredQuantity;
            }

            header.TotalReservedQuantity = totalReserved;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.ReservationNumber = resNumber;
            result.RequirementDate = header.RequirementDate;
            result.TotalReservedQuantity = totalReserved;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Errors.Add($"Reservation creation failed: {ex.Message}");
        }

        return result;
    }

    public async Task<ReservationResultDto> UpdateReservationAsync(string reservationNumber, CreateReservationDto request, Guid tenantId, string userId)
    {
        var result = new ReservationResultDto();
        var header = await _db.StockReservationHeaders
            .FirstOrDefaultAsync(h => h.ReservationNumber == reservationNumber && h.TenantId == tenantId.ToString());

        if (header == null)
        {
            result.Errors.Add($"Reservation '{reservationNumber}' not found.");
            return result;
        }

        if (header.IsCompleted || header.IsDeleted)
        {
            result.Errors.Add($"Reservation '{reservationNumber}' is {header.Status} and cannot be modified.");
            return result;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var existingItems = await _db.StockReservationItems
                .Where(i => i.ReservationHeaderId == header.Id.ToString())
                .ToListAsync();
            _db.StockReservationItems.RemoveRange(existingItems);

            header.RequirementDate = DateTime.TryParse(request.RequirementDate, out var rd) ? rd : header.RequirementDate;
            header.CostCenter = request.CostCenter;
            header.HeaderText = request.HeaderText;
            header.UpdatedAt = DateTime.UtcNow;

            var lineNum = 0;
            decimal totalReserved = 0;
            foreach (var line in request.Lines)
            {
                lineNum++;
                var item = new StockReservationItemEntity
                {
                    TenantId = tenantId.ToString(),
                    ReservationHeaderId = header.Id.ToString(),
                    LineNumber = lineNum,
                    MaterialCode = line.MaterialCode,
                    MaterialName = line.MaterialName,
                    Plant = line.Plant,
                    StorageLocation = line.StorageLocation,
                    BatchNumber = line.BatchNumber,
                    RequiredQuantity = line.RequiredQuantity,
                    UOM = line.UOM,
                    ItemText = line.ItemText,
                    Status = "Active"
                };
                _db.StockReservationItems.Add(item);
                totalReserved += line.RequiredQuantity;
            }

            header.TotalReservedQuantity = totalReserved;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.ReservationNumber = reservationNumber;
            result.TotalReservedQuantity = totalReserved;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Errors.Add($"Reservation update failed: {ex.Message}");
        }

        return result;
    }

    public async Task<ReservationResultDto> DeleteReservationAsync(string reservationNumber, string reason, Guid tenantId, string userId)
    {
        var result = new ReservationResultDto();
        var header = await _db.StockReservationHeaders
            .FirstOrDefaultAsync(h => h.ReservationNumber == reservationNumber && h.TenantId == tenantId.ToString());

        if (header == null)
        {
            result.Errors.Add($"Reservation '{reservationNumber}' not found.");
            return result;
        }

        header.IsDeleted = true;
        header.DeletedBy = userId;
        header.DeletedAt = DateTime.UtcNow;
        header.Status = "Deleted";
        header.UpdatedAt = DateTime.UtcNow;

        var items = await _db.StockReservationItems
            .Where(i => i.ReservationHeaderId == header.Id.ToString())
            .ToListAsync();
        foreach (var item in items)
        {
            item.IsDeleted = true;
            item.Status = "Deleted";
        }

        await _db.SaveChangesAsync();
        result.Success = true;
        result.ReservationNumber = reservationNumber;
        return result;
    }

    public async Task<ReservationResultDto> IssueAgainstReservationAsync(string reservationNumber, List<CreateReservationLineDto> issuedLines, Guid tenantId, string userId)
    {
        var result = new ReservationResultDto();
        var header = await _db.StockReservationHeaders
            .FirstOrDefaultAsync(h => h.ReservationNumber == reservationNumber && h.TenantId == tenantId.ToString());

        if (header == null)
        {
            result.Errors.Add($"Reservation '{reservationNumber}' not found.");
            return result;
        }

        var items = await _db.StockReservationItems
            .Where(i => i.ReservationHeaderId == header.Id.ToString() && !i.IsCompleted)
            .ToListAsync();

        decimal totalIssued = 0;
        foreach (var issued in issuedLines)
        {
            var item = items.FirstOrDefault(i => i.MaterialCode == issued.MaterialCode);
            if (item != null)
            {
                item.IssuedQuantity += issued.RequiredQuantity;
                if (item.IssuedQuantity >= item.RequiredQuantity)
                {
                    item.IsCompleted = true;
                    item.Status = "Completed";
                }
                item.UpdatedAt = DateTime.UtcNow;
                totalIssued += issued.RequiredQuantity;
            }
        }

        header.TotalIssuedQuantity += totalIssued;
        if (items.All(i => i.IsCompleted))
        {
            header.IsCompleted = true;
            header.Status = "Completed";
        }
        header.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        result.Success = true;
        result.ReservationNumber = reservationNumber;
        result.TotalReservedQuantity = totalIssued;
        return result;
    }

    public async Task<decimal> GetAvailableQuantityAsync(string materialCode, string plant, string? storageLocation, string? batchNumber, Guid tenantId)
    {
        var balance = await _db.StockBalances
            .AsNoTracking()
            .Where(s =>
                s.TenantId == tenantId &&
                s.MaterialCode == materialCode &&
                s.Plant == plant &&
                (storageLocation == null || s.StorageLocation == storageLocation) &&
                (batchNumber == null || s.BatchNumber == batchNumber) &&
                s.StockType == "Unrestricted")
            .SumAsync(s => s.Quantity);

        var reserved = await _db.StockReservationItems
            .AsNoTracking()
            .Where(i =>
                i.MaterialCode == materialCode &&
                i.Plant == plant &&
                !i.IsCompleted &&
                !i.IsDeleted)
            .SumAsync(i => i.RequiredQuantity - i.IssuedQuantity);

        return balance - reserved;
    }

    public async Task<List<ReservationHeaderDto>> GetReservationsAsync(string? materialCode, string? plant, Guid tenantId)
    {
        var query = _db.StockReservationHeaders
            .AsNoTracking()
            .Where(h => h.TenantId == tenantId.ToString() && !h.IsDeleted);

        if (!string.IsNullOrWhiteSpace(materialCode) || !string.IsNullOrWhiteSpace(plant))
        {
            var headerIds = await _db.StockReservationItems
                .Where(i =>
                    (materialCode == null || i.MaterialCode == materialCode) &&
                    (plant == null || i.Plant == plant))
                .Select(i => i.ReservationHeaderId)
                .Distinct()
                .ToListAsync();

            query = query.Where(h => headerIds.Contains(h.Id.ToString()));
        }

        var headers = await query.OrderByDescending(h => h.CreatedAt).ToListAsync();

        return headers.Select(h => new ReservationHeaderDto
        {
            ReservationNumber = h.ReservationNumber,
            RequirementDate = h.RequirementDate,
            CostCenter = h.CostCenter,
            OrderNumber = h.OrderNumber,
            Plant = h.Plant,
            TotalReservedQuantity = h.TotalReservedQuantity,
            TotalIssuedQuantity = h.TotalIssuedQuantity,
            IsCompleted = h.IsCompleted,
            Status = h.Status,
            CreatedBy = h.CreatedBy,
            CreatedAt = h.CreatedAt
        }).ToList();
    }

    private string GenerateReservationNumber(Guid tenantId)
    {
        var prefix = $"RES{DateTime.UtcNow:yyyyMMdd}";
        var count = _db.StockReservationHeaders
            .Count(h => h.ReservationNumber.StartsWith(prefix) && h.TenantId == tenantId.ToString());
        return $"{prefix}-{(count + 1):D4}";
    }
}
