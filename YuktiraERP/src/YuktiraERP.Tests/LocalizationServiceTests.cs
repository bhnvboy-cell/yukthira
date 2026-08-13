using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class LocalizationServiceTests
{
    private static YuktiraDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task GetLanguages_ReturnsDefaults_WhenEmpty()
    {
        var db = CreateInMemoryDb();
        var svc = new LocalizationService(db);

        var langs = await svc.GetLanguagesAsync();

        Assert.Contains(langs, l => l.Code == "en" && l.IsDefault);
        Assert.Contains(langs, l => l.Code == "hi");
        Assert.Contains(langs, l => l.Code == "ta");
    }

    [Fact]
    public async Task UpsertTranslations_CreatesAndUpdates()
    {
        var db = CreateInMemoryDb();
        var svc = new LocalizationService(db);
        var tenantId = Guid.NewGuid();

        var created = await svc.UpsertTranslationsAsync(tenantId, "hi", new List<YuktiraERP.Core.Interfaces.TranslationDto>
        {
            new() { Key = "common.save", Value = "à¤¸à¤¹à¥‡à¤œà¥‡à¤‚" },
            new() { Key = "common.cancel", Value = "à¤°à¤¦à¥à¤¦ à¤•à¤°à¥‡à¤‚" }
        });

        Assert.Equal(2, created.Count);
        Assert.Equal("à¤¸à¤¹à¥‡à¤œà¥‡à¤‚", created["common.save"]);

        var updated = await svc.UpsertTranslationsAsync(tenantId, "hi", new List<YuktiraERP.Core.Interfaces.TranslationDto>
        {
            new() { Key = "common.save", Value = "à¤¸à¤¹à¥‡à¤œà¥‡à¤‚!" }
        });

        Assert.Equal(2, updated.Count);
        Assert.Equal("à¤¸à¤¹à¥‡à¤œà¥‡à¤‚!", updated["common.save"]);
    }

    [Fact]
    public async Task Translations_AreScopedByTenant()
    {
        var db = CreateInMemoryDb();
        var svc = new LocalizationService(db);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await svc.UpsertTranslationsAsync(tenantA, "en", new List<YuktiraERP.Core.Interfaces.TranslationDto>
        {
            new() { Key = "dashboard.title", Value = "Dashboard" }
        });

        var other = await svc.GetTranslationsAsync(tenantB, "en");
        Assert.Empty(other);
    }

    [Fact]
    public async Task DeleteTranslation_RemovesKey()
    {
        var db = CreateInMemoryDb();
        var svc = new LocalizationService(db);
        var tenantId = Guid.NewGuid();

        await svc.UpsertTranslationsAsync(tenantId, "fr", new List<YuktiraERP.Core.Interfaces.TranslationDto>
        {
            new() { Key = "common.save", Value = "Enregistrer" }
        });
        await svc.DeleteTranslationAsync(tenantId, "fr", "common.save");

        var result = await svc.GetTranslationsAsync(tenantId, "fr");
        Assert.Empty(result);
    }
}
