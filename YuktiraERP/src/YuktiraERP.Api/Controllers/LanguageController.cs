using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.MultiTenant;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/i18n")]
[Authorize]
public class LanguageController : ControllerBase
{
    private readonly ILocalizationService _localization;
    private readonly ITenantContext _tenant;

    public LanguageController(ILocalizationService localization, ITenantContext tenant)
    {
        _localization = localization;
        _tenant = tenant;
    }

    [HttpGet("languages")]
    public async Task<IActionResult> GetLanguages()
        => Ok(new { data = await _localization.GetLanguagesAsync() });

    [HttpGet("languages/{code}/translations")]
    public async Task<IActionResult> GetTranslations(string code)
    {
        var result = await _localization.GetTranslationsAsync(_tenant.TenantId, code);
        return Ok(new { language = code, count = result.Count, data = result });
    }

    [HttpPost("languages/{code}/translations")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpsertTranslations(string code, [FromBody] List<TranslationDto> translations)
    {
        try
        {
            var result = await _localization.UpsertTranslationsAsync(_tenant.TenantId, code, translations);
            return Ok(new { success = true, count = result.Count, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("languages/{code}/translations/{key}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteTranslation(string code, string key)
    {
        await _localization.DeleteTranslationAsync(_tenant.TenantId, code, key);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
}