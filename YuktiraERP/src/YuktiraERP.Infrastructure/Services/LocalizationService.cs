using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class LocalizationService : ILocalizationService
{
    private readonly YuktiraDbContext _db;

    public LocalizationService(YuktiraDbContext db) { _db = db; }

    public async Task<List<LanguageDto>> GetLanguagesAsync()
    {
        var languages = await _db.Languages
            .OrderByDescending(l => l.IsDefault)
            .ThenBy(l => l.Name)
            .ToListAsync();
        if (languages.Count == 0)
        {
            return new List<LanguageDto>
            {
                new() { Code = "en", Name = "English", IsActive = true, IsDefault = true },
                new() { Code = "hi", Name = "हिन्दी (Hindi)", IsActive = true },
                new() { Code = "ta", Name = "தமிழ் (Tamil)", IsActive = true },
                new() { Code = "te", Name = "తెలుగు (Telugu)", IsActive = true },
                new() { Code = "kn", Name = "ಕನ್ನಡ (Kannada)", IsActive = true },
                new() { Code = "ml", Name = "മലയാളം (Malayalam)", IsActive = true },
                new() { Code = "fr", Name = "Français", IsActive = true },
                new() { Code = "es", Name = "Español", IsActive = true }
            };
        }
        return languages.Select(l => new LanguageDto
        {
            Code = l.Code,
            Name = l.Name,
            IsActive = l.IsActive,
            IsDefault = l.IsDefault
        }).ToList();
    }

    public async Task<Dictionary<string, string>> GetTranslationsAsync(Guid tenantId, string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode)) languageCode = "en";
        return await _db.Translations
            .Where(t => t.TenantId == tenantId && t.LanguageCode == languageCode)
            .ToDictionaryAsync(t => t.Key, t => t.Value);
    }

    public async Task<Dictionary<string, string>> UpsertTranslationsAsync(Guid tenantId, string languageCode, List<TranslationDto> translations)
    {
        if (string.IsNullOrWhiteSpace(languageCode)) languageCode = "en";
        if (translations == null || translations.Count == 0)
            throw new InvalidOperationException("No translations supplied");

        var existing = await _db.Translations
            .Where(t => t.TenantId == tenantId && t.LanguageCode == languageCode)
            .ToListAsync();
        var byKey = existing.ToDictionary(t => t.Key);

        foreach (var item in translations)
        {
            if (string.IsNullOrWhiteSpace(item.Key)) continue;
            if (byKey.TryGetValue(item.Key, out var entity))
            {
                entity.Value = item.Value ?? "";
                entity.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newEntity = new TranslationEntity
                {
                    TenantId = tenantId,
                    LanguageCode = languageCode,
                    Key = item.Key,
                    Value = item.Value ?? ""
                };
                _db.Translations.Add(newEntity);
                byKey[item.Key] = newEntity;
            }
        }
        await _db.SaveChangesAsync();

        return byKey.Values.ToDictionary(t => t.Key, t => t.Value);
    }

    public async Task DeleteTranslationAsync(Guid tenantId, string languageCode, string key)
    {
        var entity = await _db.Translations
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.LanguageCode == languageCode && t.Key == key);
        if (entity == null) return;
        _db.Translations.Remove(entity);
        await _db.SaveChangesAsync();
    }
}