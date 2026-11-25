using YuktiraERP.Core.Domain.Audit;

namespace YuktiraERP.Core.Domain.Security;

public class AuthorizationRule : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty; // e.g. "PurchaseOrder", "SalesOrder"
    public decimal MinThreshold { get; set; }
    public decimal MaxThreshold { get; set; } = decimal.MaxValue;
    public string RequiredRole { get; set; } = string.Empty;
    public bool RequireBudgetCheck { get; set; }
    public bool AllowOverride { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
}

public class AuthorizationEvaluationRequest
{
    public Guid UserId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public decimal Amount { get; set; }
    public Guid? CostCenterId { get; set; }
    public bool IsBudgetExceeded { get; set; }
    public IReadOnlyList<string> UserRoles { get; set; } = Array.Empty<string>();
}

public class AuthorizationEvaluationResult
{
    public bool IsAuthorized { get; set; }
    public bool RequiresHigherApproval { get; set; }
    public string? RequiredRole { get; set; }
    public bool BudgetCheckFailed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> RequiredRoles { get; set; } = new();

    public static AuthorizationEvaluationResult Approved()
        => new() { IsAuthorized = true, Reason = "Authorized under standard thresholds." };

    public static AuthorizationEvaluationResult EscalationRequired(string requiredRole, string reason, bool budgetFailed = false)
        => new()
        {
            IsAuthorized = false,
            RequiresHigherApproval = true,
            RequiredRole = requiredRole,
            RequiredRoles = new List<string> { requiredRole },
            BudgetCheckFailed = budgetFailed,
            Reason = reason
        };
}
