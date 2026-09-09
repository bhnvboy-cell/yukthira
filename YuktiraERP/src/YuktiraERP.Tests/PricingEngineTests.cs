using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class PricingEngineTests
{
    private YuktiraDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new YuktiraDbContext(options);
    }

    private async Task SeedPricingConditions(YuktiraDbContext db, Guid tenantId)
    {
        db.PricingConditions.AddRange(
            new PricingConditionEntity
            {
                TenantId = tenantId,
                ConditionType = "PR00",
                Name = "Base Price",
                Category = "BasePrice",
                CalculationType = "Fixed",
                Rate = 100m,
                Amount = 0,
                PerUnit = 1,
                Currency = "INR",
                IsPercentage = false,
                SequenceNumber = 1,
                IsActive = true
            },
            new PricingConditionEntity
            {
                TenantId = tenantId,
                ConditionType = "K004",
                Name = "Customer Discount",
                Category = "Discount",
                CalculationType = "Percentage",
                Rate = 0,
                Amount = 0,
                PerUnit = 1,
                Currency = "INR",
                IsPercentage = true,
                PercentageValue = 10m,
                SequenceNumber = 2,
                IsActive = true
            },
            new PricingConditionEntity
            {
                TenantId = tenantId,
                ConditionType = "KF00",
                Name = "Freight",
                Category = "Freight",
                CalculationType = "Fixed",
                Rate = 50m,
                Amount = 0,
                PerUnit = 1,
                Currency = "INR",
                IsPercentage = false,
                SequenceNumber = 3,
                IsActive = true
            },
            new PricingConditionEntity
            {
                TenantId = tenantId,
                ConditionType = "MWST",
                Name = "Output Tax",
                Category = "Tax",
                CalculationType = "Percentage",
                Rate = 0,
                Amount = 0,
                PerUnit = 1,
                Currency = "INR",
                IsPercentage = true,
                PercentageValue = 18m,
                SequenceNumber = 4,
                IsActive = true
            }
        );
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CalculateItemPricing_BasePriceOnly_ReturnsCorrectTotal()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();

        db.PricingConditions.Add(new PricingConditionEntity
        {
            TenantId = tenantId,
            ConditionType = "PR00",
            Name = "Base Price",
            Category = "BasePrice",
            CalculationType = "Fixed",
            Rate = 250m,
            PerUnit = 1,
            Currency = "INR",
            IsPercentage = false,
            SequenceNumber = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new PricingEngineService(db);
        var context = new PricingContext
        {
            MaterialCode = "RM-001",
            MaterialName = "Raw Material X",
            Quantity = 10,
            UOM = "KG",
            BaseUnitPrice = 250m,
            Currency = "INR"
        };

        var result = await service.CalculateItemPricingAsync(context, tenantId);

        Assert.True(result.Success);
        Assert.Equal(2500m, result.TotalBaseAmount);
        Assert.Equal(2500m, result.TotalNetAmount);
        Assert.Equal(2500m, result.TotalGrossAmount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task CalculateItemPricing_WithDiscountAndFreightAndTax_CalculatesCorrectly()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        await SeedPricingConditions(db, tenantId);

        var service = new PricingEngineService(db);
        var context = new PricingContext
        {
            MaterialCode = "RM-001",
            MaterialName = "Raw Material X",
            Quantity = 10,
            UOM = "KG",
            BaseUnitPrice = 100m,
            Currency = "INR"
        };

        var result = await service.CalculateItemPricingAsync(context, tenantId);

        Assert.True(result.Success);
        Assert.Equal(1000m, result.TotalBaseAmount);
        Assert.Equal(100m, result.TotalDiscountAmount);
        Assert.Equal(500m, result.TotalFreightAmount);
        Assert.True(result.TotalTaxAmount > 0);

        var expectedNet = 1000m - 100m + 500m;
        Assert.Equal(expectedNet, result.TotalNetAmount);

        var expectedGross = expectedNet + result.TotalTaxAmount;
        Assert.Equal(expectedGross, result.TotalGrossAmount);

        Assert.Equal(4, result.LineResult!.Steps.Count);
    }

    [Fact]
    public async Task CalculateItemPricing_PercentageBasedSteps_CalculatesCorrectly()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();

        db.PricingConditions.AddRange(
            new PricingConditionEntity
            {
                TenantId = tenantId,
                ConditionType = "PR00",
                Name = "Base Price",
                Category = "BasePrice",
                CalculationType = "Fixed",
                Rate = 500m,
                PerUnit = 1,
                Currency = "INR",
                IsPercentage = false,
                SequenceNumber = 1,
                IsActive = true
            },
            new PricingConditionEntity
            {
                TenantId = tenantId,
                ConditionType = "K005",
                Name = "Material Discount 5%",
                Category = "Discount",
                CalculationType = "Percentage",
                IsPercentage = true,
                PercentageValue = 5m,
                SequenceNumber = 2,
                IsActive = true
            },
            new PricingConditionEntity
            {
                TenantId = tenantId,
                ConditionType = "K007",
                Name = "Cash Discount 2%",
                Category = "Discount",
                CalculationType = "Percentage",
                IsPercentage = true,
                PercentageValue = 2m,
                SequenceNumber = 3,
                IsActive = true
            }
        );
        await db.SaveChangesAsync();

        var service = new PricingEngineService(db);
        var context = new PricingContext
        {
            MaterialCode = "FG-001",
            MaterialName = "Finished Product A",
            Quantity = 20,
            UOM = "EA",
            BaseUnitPrice = 500m,
            Currency = "INR"
        };

        var result = await service.CalculateItemPricingAsync(context, tenantId);

        Assert.True(result.Success);
        Assert.Equal(10000m, result.TotalBaseAmount);

        var discount1 = Math.Round(10000m * 5m / 100m, 2);
        var afterFirstDiscount = 10000m - discount1;
        var discount2 = Math.Round(afterFirstDiscount * 2m / 100m, 2);
        var totalDiscount = discount1 + discount2;

        Assert.Equal(totalDiscount, result.TotalDiscountAmount);
        Assert.Equal(10000m - totalDiscount, result.TotalNetAmount);
    }

    [Fact]
    public async Task CalculateItemPricing_NoConditions_ReturnsError()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();

        var service = new PricingEngineService(db);
        var context = new PricingContext
        {
            MaterialCode = "RM-001",
            Quantity = 5,
            BaseUnitPrice = 100m
        };

        var result = await service.CalculateItemPricingAsync(context, tenantId);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task CalculateItemPricing_InactiveCondition_Skipped()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();

        db.PricingConditions.AddRange(
            new PricingConditionEntity
            {
                TenantId = tenantId,
                ConditionType = "PR00",
                Name = "Base Price",
                Category = "BasePrice",
                Rate = 100m,
                PerUnit = 1,
                Currency = "INR",
                SequenceNumber = 1,
                IsActive = true
            },
            new PricingConditionEntity
            {
                TenantId = tenantId,
                ConditionType = "K004",
                Name = "Old Discount",
                Category = "Discount",
                IsPercentage = true,
                PercentageValue = 50m,
                SequenceNumber = 2,
                IsActive = false
            }
        );
        await db.SaveChangesAsync();

        var service = new PricingEngineService(db);
        var context = new PricingContext
        {
            MaterialCode = "RM-001",
            Quantity = 10,
            BaseUnitPrice = 100m,
            Currency = "INR"
        };

        var result = await service.CalculateItemPricingAsync(context, tenantId);

        Assert.True(result.Success);
        Assert.Equal(0m, result.TotalDiscountAmount);
        Assert.Equal(1000m, result.TotalNetAmount);
    }

    [Fact]
    public async Task CalculateItemPricing_FixedAmountPerUnit_CalculatesCorrectly()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();

        db.PricingConditions.AddRange(
            new PricingConditionEntity
            {
                TenantId = tenantId,
                ConditionType = "PR00",
                Name = "Base Price",
                Category = "BasePrice",
                Rate = 100m,
                PerUnit = 1,
                Currency = "INR",
                SequenceNumber = 1,
                IsActive = true
            },
            new PricingConditionEntity
            {
                TenantId = tenantId,
                ConditionType = "KF00",
                Name = "Freight per 10 KG",
                Category = "Freight",
                CalculationType = "Fixed",
                Rate = 25m,
                PerUnit = 10,
                Currency = "INR",
                SequenceNumber = 2,
                IsActive = true
            }
        );
        await db.SaveChangesAsync();

        var service = new PricingEngineService(db);
        var context = new PricingContext
        {
            MaterialCode = "RM-001",
            Quantity = 50,
            UOM = "KG",
            BaseUnitPrice = 100m,
            Currency = "INR"
        };

        var result = await service.CalculateItemPricingAsync(context, tenantId);

        Assert.True(result.Success);
        Assert.Equal(5000m, result.TotalBaseAmount);
        Assert.Equal(125m, result.TotalFreightAmount);
    }

    [Fact]
    public async Task GetActiveConditions_ReturnsOnlyActiveConditions()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        await SeedPricingConditions(db, tenantId);

        var inactive = new PricingConditionEntity
        {
            TenantId = tenantId,
            ConditionType = "OLD",
            Name = "Old Condition",
            Category = "Discount",
            IsActive = false,
            SequenceNumber = 99
        };
        db.PricingConditions.Add(inactive);
        await db.SaveChangesAsync();

        var service = new PricingEngineService(db);
        var conditions = await service.GetActiveConditionsAsync(tenantId);

        Assert.Equal(4, conditions.Count);
        Assert.All(conditions, c => Assert.True(c.IsActive));
    }

    [Fact]
    public async Task CreateCondition_AddsToDatabase()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var service = new PricingEngineService(db);

        var dto = new PricingConditionDto
        {
            ConditionType = "PR00",
            Name = "Test Base Price",
            Category = PricingConditionType.BasePrice,
            Rate = 150m,
            Currency = "INR",
            SequenceNumber = 1,
            IsActive = true
        };

        var result = await service.CreateConditionAsync(tenantId, dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(tenantId, result.TenantId);

        var saved = await db.PricingConditions.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test Base Price", saved!.Name);
    }
}

public class SalesBillingServiceTests
{
    private YuktiraDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task ReleaseBillingDocument_BillingNotFound_ReturnsError()
    {
        using var db = CreateInMemoryDb();
        var mockPricing = new MockPricingEngine();
        var service = new SalesBillingService(db, mockPricing);

        var result = await service.ReleaseBillingDocumentToFIAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ReleaseBillingDocument_AlreadyReleased_ReturnsError()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var billingId = Guid.NewGuid();

        db.BillingDocuments.Add(new BillingDocumentEntity
        {
            Id = billingId,
            TenantId = tenantId,
            DocumentNumber = "VF-001",
            Date = DateTime.UtcNow,
            CustomerName = "Test Customer",
            Amount = 1000m,
            Status = "FI_Posted"
        });
        await db.SaveChangesAsync();

        var mockPricing = new MockPricingEngine();
        var service = new SalesBillingService(db, mockPricing);

        var result = await service.ReleaseBillingDocumentToFIAsync(billingId, tenantId);

        Assert.False(result.Success);
        Assert.Contains("already released", result.Errors[0]);
    }

    [Fact]
    public async Task ReleaseBillingDocument_CreatesJournalEntryAndAREntry()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var billingId = Guid.NewGuid();

        db.BillingDocuments.Add(new BillingDocumentEntity
        {
            Id = billingId,
            TenantId = tenantId,
            DocumentNumber = "VF-001",
            Date = DateTime.UtcNow,
            SoNumber = "SO-001",
            CustomerName = "Acme Corp",
            Amount = 5000m,
            Status = "Unpaid"
        });
        db.Customers.Add(new CustomerEntity
        {
            Code = "CUST-001",
            Name = "Acme Corp",
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var mockPricing = new MockPricingEngine
        {
            MockResult = new PricingResult
            {
                Success = true,
                Currency = "INR",
                TotalBaseAmount = 5000m,
                TotalDiscountAmount = 500m,
                TotalFreightAmount = 200m,
                TotalTaxAmount = 846m,
                TotalNetAmount = 4700m,
                TotalGrossAmount = 5546m
            }
        };

        var service = new SalesBillingService(db, mockPricing);
        var result = await service.ReleaseBillingDocumentToFIAsync(billingId, tenantId);

        Assert.True(result.Success);
        Assert.Equal("VF-001", result.BillingDocumentNumber);
        Assert.StartsWith("FB", result.FiDocumentNumber);
        Assert.Equal(5000m, result.BaseAmount);
        Assert.Equal(200m, result.FreightAmount);
        Assert.Equal(846m, result.TotalTaxAmount);
        Assert.Equal(5546m, result.TotalGrossAmount);
        Assert.NotEmpty(result.JournalLines);

        var arLine = result.JournalLines.First(l => l.AccountCode == "1400");
        Assert.Equal(5546m, arLine.DebitAmount);

        var revenueLine = result.JournalLines.First(l => l.AccountCode == "4000");
        Assert.Equal(5000m, revenueLine.CreditAmount);

        var freightLine = result.JournalLines.First(l => l.AccountCode == "4100");
        Assert.Equal(200m, freightLine.CreditAmount);

        var taxLine = result.JournalLines.First(l => l.AccountCode == "2300");
        Assert.Equal(846m, taxLine.CreditAmount);

        var journalEntries = await db.UniversalJournals
            .Where(j => j.DocumentNumber == result.FiDocumentNumber)
            .ToListAsync();
        Assert.Equal(4, journalEntries.Count);

        var arEntry = await db.AREntries
            .FirstOrDefaultAsync(a => a.DocumentNumber == result.FiDocumentNumber);
        Assert.NotNull(arEntry);
        Assert.Equal(5546m, arEntry!.Amount);

        var updatedBilling = await db.BillingDocuments.FindAsync(billingId);
        Assert.Equal("FI_Posted", updatedBilling!.Status);
    }

    [Fact]
    public async Task ReleaseBillingDocument_VerifiesTraceabilityIds()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var billingId = Guid.NewGuid();

        db.BillingDocuments.Add(new BillingDocumentEntity
        {
            Id = billingId,
            TenantId = tenantId,
            DocumentNumber = "VF-100",
            Date = DateTime.UtcNow,
            SoNumber = "SO-200",
            CustomerName = "Beta Inc",
            Amount = 1000m,
            Status = "Unpaid"
        });
        await db.SaveChangesAsync();

        var mockPricing = new MockPricingEngine
        {
            MockResult = new PricingResult
            {
                Success = true,
                Currency = "INR",
                TotalBaseAmount = 1000m,
                TotalNetAmount = 1000m,
                TotalGrossAmount = 1000m
            }
        };

        var service = new SalesBillingService(db, mockPricing);
        var result = await service.ReleaseBillingDocumentToFIAsync(billingId, tenantId);

        Assert.True(result.Success);
        Assert.Equal("SO-200", result.SalesOrderId);
        Assert.False(string.IsNullOrEmpty(result.FiJournalId));
        Assert.StartsWith("FB", result.FiJournalId);
    }

    [Fact]
    public async Task GetBillingDocumentDetail_ReturnsCompleteDetail()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var billingId = Guid.NewGuid();

        db.BillingDocuments.Add(new BillingDocumentEntity
        {
            Id = billingId,
            TenantId = tenantId,
            DocumentNumber = "VF-050",
            Date = DateTime.UtcNow,
            SoNumber = "SO-050",
            CustomerName = "Gamma Ltd",
            Amount = 3000m,
            Status = "FI_Posted"
        });
        db.BillingDocumentLines.Add(new BillingDocumentLineEntity
        {
            BillingDocumentId = billingId,
            LineNumber = 1,
            MaterialCode = "FG-001",
            MaterialName = "Product A",
            Quantity = 10,
            UOM = "EA",
            UnitPrice = 300m,
            LineAmount = 3000m,
            NetAmount = 3000m
        });
        await db.SaveChangesAsync();

        var mockPricing = new MockPricingEngine();
        var service = new SalesBillingService(db, mockPricing);

        var detail = await service.GetBillingDocumentDetailAsync(billingId, tenantId);

        Assert.Equal("VF-050", detail.DocumentNumber);
        Assert.Equal("SO-050", detail.SoNumber);
        Assert.Equal("Gamma Ltd", detail.CustomerName);
        Assert.Equal(3000m, detail.Amount);
        Assert.Single(detail.Lines);
        Assert.Equal("FG-001", detail.Lines[0].MaterialCode);
    }
}

internal class MockPricingEngine : IPricingEngineService
{
    public PricingResult MockResult { get; set; } = new() { Success = true, Currency = "INR" };

    public Task<PricingResult> CalculateItemPricingAsync(PricingContext context, Guid tenantId)
        => Task.FromResult(MockResult);

    public Task<List<PricingConditionDto>> GetActiveConditionsAsync(Guid tenantId)
        => Task.FromResult(new List<PricingConditionDto>());

    public Task<PricingConditionDto> CreateConditionAsync(Guid tenantId, PricingConditionDto condition)
        => Task.FromResult(condition);
}
