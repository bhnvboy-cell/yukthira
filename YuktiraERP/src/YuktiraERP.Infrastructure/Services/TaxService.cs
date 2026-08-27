using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class TaxService : ITaxService
{
    private readonly YuktiraDbContext _db;

    public TaxService(YuktiraDbContext db) { _db = db; }

    public async Task<List<TaxCodeDto>> GetTaxCodesAsync(Guid tenantId)
    {
        return await _db.TaxCodes
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Code)
            .Select(t => new TaxCodeDto
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                Rate = t.Rate,
                TaxType = t.TaxType,
                TaxAccountCode = t.TaxAccountCode,
                IsCompound = t.IsCompound,
                IsActive = t.IsActive
            })
            .ToListAsync();
    }

    public async Task<TaxCodeDto> CreateTaxCodeAsync(Guid tenantId, TaxCodeDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new InvalidOperationException("Tax code is required");
        if (request.Rate < 0 || request.Rate > 100)
            throw new InvalidOperationException("Tax rate must be between 0 and 100");

        var exists = await _db.TaxCodes.AnyAsync(t => t.TenantId == tenantId && t.Code == request.Code);
        if (exists)
            throw new InvalidOperationException($"Tax code {request.Code} already exists");

        var entity = new TaxCodeEntity
        {
            TenantId = tenantId,
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            Rate = request.Rate,
            TaxType = request.TaxType,
            TaxAccountCode = request.TaxAccountCode,
            IsCompound = request.IsCompound,
            IsActive = request.IsActive
        };
        _db.TaxCodes.Add(entity);
        await _db.SaveChangesAsync();

        return new TaxCodeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Rate = entity.Rate,
            TaxType = entity.TaxType,
            TaxAccountCode = entity.TaxAccountCode,
            IsCompound = entity.IsCompound,
            IsActive = entity.IsActive
        };
    }

    public async Task<TaxCodeDto?> UpdateTaxCodeAsync(Guid tenantId, Guid id, TaxCodeDto request)
    {
        var entity = await _db.TaxCodes.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == id);
        if (entity == null) return null;

        if (request.Rate < 0 || request.Rate > 100)
            throw new InvalidOperationException("Tax rate must be between 0 and 100");

        entity.Name = request.Name;
        entity.Rate = request.Rate;
        entity.TaxType = request.TaxType;
        entity.TaxAccountCode = request.TaxAccountCode;
        entity.IsCompound = request.IsCompound;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new TaxCodeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Rate = entity.Rate,
            TaxType = entity.TaxType,
            TaxAccountCode = entity.TaxAccountCode,
            IsCompound = entity.IsCompound,
            IsActive = entity.IsActive
        };
    }

    public async Task DeleteTaxCodeAsync(Guid tenantId, Guid id)
    {
        var entity = await _db.TaxCodes.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == id);
        if (entity == null) return;
        _db.TaxCodes.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<TaxBreakdownDto> CalculateAsync(Guid tenantId, TaxCalculationRequest request)
    {
        var codes = await LoadCodesAsync(tenantId, request.Lines.Select(l => l.TaxCode));
        var breakdown = Compute(codes, request);
        return breakdown;
    }

    public async Task<TaxBreakdownDto> PostInvoiceAsync(Guid tenantId, TaxCalculationRequest request)
    {
        var codes = await LoadCodesAsync(tenantId, request.Lines.Select(l => l.TaxCode));
        var breakdown = Compute(codes, request);

        // Post the AR/AP entry for the gross amount
        var docNum = string.IsNullOrEmpty(request.DocumentNumber)
            ? $"{(request.DocumentType == "AP" ? "INV-AP" : "INV-AR")}-{request.Date:yyyyMMdd}-{Guid.NewGuid():N}"[..20]
            : request.DocumentNumber;

        if (request.DocumentType == "AP")
        {
            _db.APEntries.Add(new APEntryEntity
            {
                TenantId = tenantId,
                DocumentNumber = docNum,
                Date = request.Date,
                VendorName = request.PartyName,
                Amount = breakdown.TotalGross,
                Status = "Open"
            });
        }
        else
        {
            _db.AREntries.Add(new AREntryEntity
            {
                TenantId = tenantId,
                DocumentNumber = docNum,
                Date = request.Date,
                CustomerName = request.PartyName,
                Amount = breakdown.TotalGross,
                Status = "Open"
            });
        }

        // Persist tax transactions per tax code
        foreach (var line in breakdown.Lines)
        {
            if (line.TaxAmount == 0) continue;
            _db.TaxTransactions.Add(new TaxTransactionEntity
            {
                TenantId = tenantId,
                DocumentNumber = docNum,
                DocumentType = request.DocumentType,
                PartyName = request.PartyName,
                TaxCode = line.TaxCode,
                TaxName = line.TaxName,
                Rate = line.Rate,
                NetAmount = line.NetAmount,
                TaxAmount = line.TaxAmount,
                GrossAmount = line.GrossAmount,
                Date = request.Date,
                Status = "Posted"
            });
        }

        await _db.SaveChangesAsync();
        return breakdown;
    }

    public async Task<List<TaxTransactionDto>> GetTaxTransactionsAsync(Guid tenantId, int limit = 100)
    {
        return await _db.TaxTransactions
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.Date)
            .Take(limit)
            .Select(t => new TaxTransactionDto
            {
                Id = t.Id,
                DocumentNumber = t.DocumentNumber,
                DocumentType = t.DocumentType,
                PartyName = t.PartyName,
                TaxCode = t.TaxCode,
                TaxName = t.TaxName,
                Rate = t.Rate,
                NetAmount = t.NetAmount,
                TaxAmount = t.TaxAmount,
                GrossAmount = t.GrossAmount,
                Date = t.Date,
                Status = t.Status
            })
            .ToListAsync();
    }

    private async Task<Dictionary<string, TaxCodeEntity>> LoadCodesAsync(Guid tenantId, IEnumerable<string> codes)
    {
        var distinct = codes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        var result = new Dictionary<string, TaxCodeEntity>();
        if (distinct.Count == 0) return result;

        foreach (var code in distinct)
        {
            var entity = await _db.TaxCodes
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Code == code);
            if (entity == null)
                throw new InvalidOperationException($"Tax code {code} not found for this tenant. Create it first.");
            result[code] = entity;
        }
        return result;
    }

    private static TaxBreakdownDto Compute(Dictionary<string, TaxCodeEntity> codes, TaxCalculationRequest request)
    {
        var lines = new List<TaxLineResult>();
        decimal totalNet = 0, totalTax = 0, totalGross = 0;
        decimal priorNonCompoundTax = 0m;

        foreach (var line in request.Lines)
        {
            if (line.NetAmount == 0) continue;
            if (string.IsNullOrWhiteSpace(line.TaxCode))
                throw new InvalidOperationException("Each line must specify a tax code");

            var code = codes[line.TaxCode];
            var taxBase = code.IsCompound ? line.NetAmount + priorNonCompoundTax : line.NetAmount;
            var tax = taxBase * code.Rate / 100m;
            if (!code.IsCompound)
                priorNonCompoundTax += tax;
            totalNet += line.NetAmount;
            totalTax += tax;
            totalGross += line.NetAmount + tax;

            lines.Add(new TaxLineResult
            {
                TaxCode = code.Code,
                TaxName = code.Name,
                Rate = code.Rate,
                NetAmount = line.NetAmount,
                TaxAmount = tax,
                GrossAmount = line.NetAmount + tax
            });
        }

        return new TaxBreakdownDto
        {
            DocumentNumber = request.DocumentNumber,
            DocumentType = request.DocumentType,
            PartyName = request.PartyName,
            TotalNet = totalNet,
            TotalTax = totalTax,
            TotalGross = totalGross,
            Lines = lines
        };
    }
}