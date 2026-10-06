namespace YuktiraERP.Core.Dtos;

public class MdgSubmitRequest
{
    public string EntityName { get; set; } = "";
    public string StagingPayload { get; set; } = "{}";
    public string RequestedBy { get; set; } = "";
}

public class MdgApprovalRequest
{
    public string ApprovedBy { get; set; } = "";
    public string? Notes { get; set; }
}

public class MdgRejectionRequest
{
    public string RejectedBy { get; set; } = "";
    public string Reason { get; set; } = "";
}

public class MdgChangeRequestDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string RequestNumber { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string? EntityId { get; set; }
    public string StagingPayload { get; set; } = "{}";
    public string Status { get; set; } = "";
    public string RequestedBy { get; set; } = "";
    public string? ApprovedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class MdgValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class MdgCreateDraftRequest
{
    public string EntityName { get; set; } = "";
    public string StagingPayload { get; set; } = "{}";
    public string? Notes { get; set; }
    public string? RequestedBy { get; set; }
}

public class MdgPagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class MdgDiffDto
{
    public string OriginalJson { get; set; } = "{}";
    public string ProposedJson { get; set; } = "{}";
    public List<MdgFieldChangeDto> Changes { get; set; } = new();
}

public class MdgFieldChangeDto
{
    public string Field { get; set; } = "";
    public string? From { get; set; }
    public string? To { get; set; }
    public string ChangeType { get; set; } = "";
}

public class MdgAuditTrailDto
{
    public Guid Id { get; set; }
    public Guid ChangeRequestId { get; set; }
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Comment { get; set; } = "";
    public string Hash { get; set; } = "";
    public string? PreviousHash { get; set; }
    public DateTime CreatedAt { get; set; }
}
