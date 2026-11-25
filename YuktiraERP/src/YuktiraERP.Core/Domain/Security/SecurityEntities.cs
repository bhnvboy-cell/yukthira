using YuktiraERP.Core.Domain.Audit;

namespace YuktiraERP.Core.Domain.Security;

public enum SodSeverity
{
    Strict_Block = 1,
    Alert_Only = 2
}

public class IdentityUser : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; } = false;
    public int FailedAttempts { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
}

public class IdentityRole : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
}

public class IdentityPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Module { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class IdentityUserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? AssignedByUserId { get; set; }
}

public class IdentityRolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SodRuleEntity : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string FirstRoleOrPermission { get; set; } = string.Empty;
    public string SecondRoleOrPermission { get; set; } = string.Empty;
    public SodSeverity Severity { get; set; } = SodSeverity.Strict_Block;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
}

public class SodViolation
{
    public string RuleCode { get; set; } = string.Empty;
    public string FirstConflict { get; set; } = string.Empty;
    public string SecondConflict { get; set; } = string.Empty;
    public SodSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SodConflictResult
{
    public bool HasConflict => Violations.Count > 0;
    public bool HasBlockingConflict => Violations.Any(v => v.Severity == SodSeverity.Strict_Block);
    public List<SodViolation> Violations { get; set; } = new();

    public static SodConflictResult Clean() => new();

    public static SodConflictResult Conflict(string ruleCode, string first, string second, SodSeverity severity, string message)
    {
        var result = new SodConflictResult();
        result.Violations.Add(new SodViolation
        {
            RuleCode = ruleCode,
            FirstConflict = first,
            SecondConflict = second,
            Severity = severity,
            Message = message
        });
        return result;
    }
}
