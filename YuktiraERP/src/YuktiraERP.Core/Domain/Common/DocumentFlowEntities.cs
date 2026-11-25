using System;
using YuktiraERP.Core.Domain.Audit;

namespace YuktiraERP.Core.Domain.Common;

public enum DocumentFlowType
{
    Predecessor = 1,
    Successor = 2,
    Reference = 3,
    Reversal = 4
}

public class DocumentReference : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SourceDocType { get; set; } = string.Empty;
    public Guid SourceDocId { get; set; }
    public string TargetDocType { get; set; } = string.Empty;
    public Guid TargetDocId { get; set; }
    public DocumentFlowType LinkType { get; set; } = DocumentFlowType.Successor;
    public string? Notes { get; set; }
}

public enum GenericDocumentState
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Posted = 5,
    Closed = 6,
    Cancelled = 7,
    Reversed = 8
}
