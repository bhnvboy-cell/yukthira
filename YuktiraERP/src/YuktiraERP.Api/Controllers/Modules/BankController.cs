using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/v1/fi/bank")]
[Authorize]
public class BankController : ControllerBase
{
    private readonly IBankService _bankService;

    public BankController(IBankService bankService)
    {
        _bankService = bankService;
    }

    [HttpPost("statement/import")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> ImportStatement(IFormFile file, [FromQuery] string format)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        using var stream = file.OpenReadStream();
        Core.Interfaces.BankStatement statement;

        switch (format?.ToUpper())
        {
            case "OFX":
                statement = await _bankService.ImportOfxStatementAsync(stream);
                break;
            case "MT940":
                using (var reader = new StreamReader(stream))
                {
                    var content = await reader.ReadToEndAsync();
                    statement = await _bankService.ImportMt940StatementAsync(content);
                }
                break;
            case "CSV":
                statement = await _bankService.ImportCsvStatementAsync(stream);
                break;
            default:
                return BadRequest(new { error = "Unsupported format. Use OFX, MT940, or CSV" });
        }

        return Ok(statement);
    }

    [HttpPost("statement/{id:guid}/auto-match")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> AutoMatch(Guid id)
    {
        var result = await _bankService.AutoMatchAsync(id);
        return Ok(result);
    }

    [HttpPost("reconcile")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Reconcile([FromBody] ReconcileRequest request)
    {
        var result = await _bankService.ReconcileAsync(request.AccountId, request.StatementId, request.MatchedIds);
        return Ok(result);
    }

    [HttpGet("accounts/{id:guid}/unmatched")]
    public async Task<IActionResult> GetUnmatchedTransactions(Guid id)
    {
        var result = await _bankService.GetUnmatchedTransactionsAsync(id);
        return Ok(result);
    }
}

public class ReconcileRequest
{
    public Guid AccountId { get; set; }
    public Guid StatementId { get; set; }
    public List<Guid> MatchedIds { get; set; } = new();
}
