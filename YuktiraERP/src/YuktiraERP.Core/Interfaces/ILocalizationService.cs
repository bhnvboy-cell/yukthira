namespace YuktiraERP.Core.Interfaces;

public class LanguageDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
}

public class TranslationDto
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public interface ILocalizationService
{
    Task<List<LanguageDto>> GetLanguagesAsync();
    Task<Dictionary<string, string>> GetTranslationsAsync(Guid tenantId, string languageCode);
    Task<Dictionary<string, string>> UpsertTranslationsAsync(Guid tenantId, string languageCode, List<TranslationDto> translations);
    Task DeleteTranslationAsync(Guid tenantId, string languageCode, string key);
}