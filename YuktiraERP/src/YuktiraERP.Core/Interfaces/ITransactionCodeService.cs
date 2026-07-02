using YuktiraERP.Core.Domain.Transaction;

namespace YuktiraERP.Core.Interfaces;

public class TransactionCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public TransactionGroup Group { get; set; }
    public string Route { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-asterisk";
    public int SortOrder { get; set; }
    public TransactionStatus Status { get; set; }
    public bool IsSystem { get; set; }
    public string RequiredRole { get; set; } = string.Empty;
    public string Params { get; set; } = "{}";
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExecuteTransactionRequest
{
    public string Code { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}

public class ExecuteTransactionResult
{
    public ExecutionStatus Status { get; set; }
    public string RedirectUrl { get; set; } = string.Empty;
    public string? Message { get; set; }
    public long DurationMs { get; set; }
    public object? Data { get; set; }
}

public class TransactionLogDto
{
    public Guid Id { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public ExecutionStatus Status { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TransactionPermissionDto
{
    public Guid Id { get; set; }
    public Guid TransactionCodeId { get; set; }
    public string PrincipalType { get; set; } = "Role";
    public string PrincipalValue { get; set; } = string.Empty;
    public bool CanAccess { get; set; } = true;
}

public interface ITransactionCodeService
{
    Task<List<TransactionCodeDto>> GetAllAsync(string? module = null, TransactionGroup? group = null, string? search = null);
    Task<TransactionCodeDto?> GetByIdAsync(Guid id);
    Task<TransactionCodeDto?> GetByCodeAsync(string code);
    Task<TransactionCodeDto> CreateAsync(TransactionCodeDto dto);
    Task<TransactionCodeDto?> UpdateAsync(Guid id, TransactionCodeDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<List<TransactionCodeDto>> SearchAsync(string query);
    Task<ExecuteTransactionResult> ExecuteAsync(string code, Guid? userId, Guid? tenantId, string? ipAddress, Dictionary<string, object>? parameters = null);
    Task<List<TransactionCodeDto>> GetFavoritesAsync(Guid userId);
    Task ToggleFavoriteAsync(Guid userId, Guid transactionCodeId);
    Task<List<TransactionCodeDto>> GetRecentAsync(Guid userId, int count = 10);
    Task<List<TransactionCodeDto>> GetPermittedCodesAsync(Guid? userId, string? role);
    Task<bool> ValidateAccessAsync(string code, Guid? userId, string? role);
    Task<TransactionPermissionDto?> SetPermissionAsync(TransactionPermissionDto dto);
    Task<List<TransactionPermissionDto>> GetPermissionsAsync(Guid transactionCodeId);
    Task<List<TransactionLogDto>> GetLogAsync(Guid? userId = null, string? code = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50);
}
