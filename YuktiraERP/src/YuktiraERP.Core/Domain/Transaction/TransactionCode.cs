namespace YuktiraERP.Core.Domain.Transaction;

public class TransactionCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public TransactionGroup Group { get; set; } = TransactionGroup.Transactions;
    public string Route { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-asterisk";
    public int SortOrder { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Active;
    public bool IsSystem { get; set; }
    public string RequiredRole { get; set; } = string.Empty;
    public string Params { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
