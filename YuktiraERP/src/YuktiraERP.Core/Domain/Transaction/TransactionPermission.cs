namespace YuktiraERP.Core.Domain.Transaction;

public class TransactionPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionCodeId { get; set; }
    public string PrincipalType { get; set; } = "Role";
    public string PrincipalValue { get; set; } = string.Empty;
    public bool CanAccess { get; set; } = true;
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
