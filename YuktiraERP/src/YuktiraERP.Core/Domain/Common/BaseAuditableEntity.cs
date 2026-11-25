using System;
using YuktiraERP.Core.Domain.Audit;

namespace YuktiraERP.Core.Domain.Common;

public abstract class BaseAuditableEntity : IAuditable
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
}
