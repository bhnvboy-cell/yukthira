using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace YuktiraERP.Mes.Core.Domain.Common;

/// <summary>
/// Base entity for all MES tables in the yuktira_mes schema.
/// Aligns with Section 1.5 cross-cutting conventions:
/// tenant_id, plant_code, created_at, created_by, row_version, source_id.
/// </summary>
public abstract class MesEntityBase
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    [MaxLength(32)]
    public string PlantCode { get; set; } = "PLANT01";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(128)]
    public string CreatedBy { get; set; } = "system";

    public long RowVersion { get; set; } = 1;

    [MaxLength(64)]
    public string SourceId { get; set; } = "MES";
}

/// <summary>
/// Marker interface for append-only audit-sensitive tables
/// (documents, e-signatures, holds, recipe downloads, as-built records).
/// </summary>
public interface IMesAuditAppendOnly
{
    string RecordHash { get; set; }
}

public static class RecordHashHelper
{
    public static string ComputeSha256(params object?[] fields)
    {
        var payload = string.Join("|", fields.Select(f => f switch
        {
            null => "",
            DateTime dt => dt.ToUniversalTime().ToString("O"),
            _ => f.ToString() ?? ""
        }));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
