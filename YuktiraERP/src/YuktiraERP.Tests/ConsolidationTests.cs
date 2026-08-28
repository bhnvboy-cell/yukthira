using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Tests;

public class ConsolidationTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task CON01_CreateGroup_WithEntities()
    {
        var db = CreateDb();
        var service = new ConsolidationService(db);

        var groupResult = await service.CreateConsolidationGroupAsync(new ConsolidationGroupCreateRequest
        {
            GroupName = "Global Group FY2026",
            GroupDescription = "Consolidation for all entities",
            ReportingCurrency = "USD",
            FiscalYear = "2026"
        });

        Assert.True(groupResult.Success);
        Assert.NotEmpty(groupResult.GroupId);
        Assert.Equal("Global Group FY2026", groupResult.GroupName);

        var entity1 = await service.AddEntityAsync(new ConsolidationEntityAddRequest
        {
            GroupId = groupResult.GroupId,
            EntityCode = "ENT-USA",
            EntityName = "US Operations",
            FunctionalCurrency = "USD",
            OwnershipPercentage = 100,
            CountryCode = "US"
        });

        var entity2 = await service.AddEntityAsync(new ConsolidationEntityAddRequest
        {
            GroupId = groupResult.GroupId,
            EntityCode = "ENT-EUR",
            EntityName = "European Operations",
            FunctionalCurrency = "EUR",
            OwnershipPercentage = 80,
            CountryCode = "DE"
        });

        Assert.True(entity1.Success);
        Assert.True(entity2.Success);

        var entities = await db.ConsolidationEntities
            .Where(e => e.GroupId.ToString() == groupResult.GroupId)
            .ToListAsync();
        Assert.Equal(2, entities.Count);
    }

    [Fact]
    public async Task CON02_CurrencyTranslation_Calculates()
    {
        var db = CreateDb();
        var service = new ConsolidationService(db);

        var groupResult = await service.CreateConsolidationGroupAsync(new ConsolidationGroupCreateRequest
        {
            GroupName = "Translation Test",
            ReportingCurrency = "USD",
            FiscalYear = "2026"
        });

        var entityResult = await service.AddEntityAsync(new ConsolidationEntityAddRequest
        {
            GroupId = groupResult.GroupId,
            EntityCode = "ENT-EUR",
            EntityName = "European Ops",
            FunctionalCurrency = "EUR",
            OwnershipPercentage = 100,
            CountryCode = "DE"
        });

        var entity = await db.ConsolidationEntities.FirstOrDefaultAsync(e => e.Id.ToString() == entityResult.EntityId);
        entity.LocalCurrencyRevenue = 1000000;
        entity.LocalCurrencyCost = 600000;
        entity.TranslatedRevenue = 0;
        await db.SaveChangesAsync();

        var translationResult = await service.RunCurrencyTranslationAsync(new CurrencyTranslationRequest
        {
            GroupId = groupResult.GroupId,
            FiscalYear = "2026",
            FiscalPeriod = 12,
            TranslationMethod = "Closing"
        });

        Assert.True(translationResult.Success);
        Assert.Equal(1, translationResult.EntitiesTranslated);
        Assert.Equal("USD", translationResult.ReportingCurrency);
        Assert.True(translationResult.TotalTranslationDifference >= 0);
        Assert.True(translationResult.Details.Count > 0);

        var updatedEntity = await db.ConsolidationEntities.FirstOrDefaultAsync(e => e.Id.ToString() == entityResult.EntityId);
        Assert.True(updatedEntity.TranslatedRevenue > 0);
    }

    [Fact]
    public async Task CON03_Elimination_InterCompany()
    {
        var db = CreateDb();
        var service = new ConsolidationService(db);

        var groupResult = await service.CreateConsolidationGroupAsync(new ConsolidationGroupCreateRequest
        {
            GroupName = "Elimination Test",
            ReportingCurrency = "USD",
            FiscalYear = "2026"
        });

        db.InterCompanyTransactions.Add(new InterCompanyTransactionEntity
        {
            FromEntityCode = "ENT-USA", FromEntityName = "US Ops",
            ToEntityCode = "ENT-EUR", ToEntityName = "EU Ops",
            TransactionType = "Intercompany Sale",
            Amount = 50000, Currency = "USD", AmountGroupCurrency = 50000,
            ExchangeRate = 1.0m, IsEliminated = false
        });
        db.InterCompanyTransactions.Add(new InterCompanyTransactionEntity
        {
            FromEntityCode = "ENT-EUR", FromEntityName = "EU Ops",
            ToEntityCode = "ENT-USA", ToEntityName = "US Ops",
            TransactionType = "Intercompany Service",
            Amount = 25000, Currency = "USD", AmountGroupCurrency = 25000,
            ExchangeRate = 1.0m, IsEliminated = false
        });
        await db.SaveChangesAsync();

        var result = await service.RunEliminationsAsync(new EliminationsRunRequest
        {
            GroupId = groupResult.GroupId,
            FiscalYear = "2026",
            FiscalPeriod = 12
        });

        Assert.True(result.Success);
        Assert.Equal(2, result.EliminationsProcessed);
        Assert.Equal(75000, result.TotalEliminationAmount);
        Assert.True(result.Eliminations.Count == 2);
        Assert.True(result.Eliminations.All(e => e.EliminationType == "InterCompany"));

        var eliminations = await db.ConsolidationEliminations
            .Where(e => e.GroupId.ToString() == groupResult.GroupId)
            .ToListAsync();
        Assert.Equal(2, eliminations.Count);
        Assert.True(eliminations.All(e => e.Status == "Posted"));

        var icTransactions = await db.InterCompanyTransactions.ToListAsync();
        Assert.True(icTransactions.All(t => t.IsEliminated));
    }
}
