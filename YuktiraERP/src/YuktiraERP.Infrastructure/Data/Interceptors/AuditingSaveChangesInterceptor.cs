using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YuktiraERP.Core.Domain.Audit;

namespace YuktiraERP.Infrastructure.Data.Interceptors;

public class AuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> SensitiveFieldSubstrings = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "hash",
        "secret",
        "token",
        "credential",
        "authkey",
        "privatekey",
        "cvv",
        "pin",
        "apikey",
        "accesstoken",
        "refreshtoken"
    };

    private readonly ICurrentActorContext _actorContext;
    private List<AuditEntryContext>? _pendingAuditEntries;

    public AuditingSaveChangesInterceptor(ICurrentActorContext actorContext)
    {
        _actorContext = actorContext ?? throw new ArgumentNullException(nameof(actorContext));
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        BeforeSaveChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        BeforeSaveChanges(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        AfterSaveChanges(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await AfterSaveChangesAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void BeforeSaveChanges(DbContext? context)
    {
        if (context == null) return;

        var now = DateTime.UtcNow;
        var userId = _actorContext.UserId;
        var tenantId = _actorContext.TenantId;
        var ip = _actorContext.IpAddress;

        _pendingAuditEntries = new List<AuditEntryContext>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditChangeLog) continue; // Do not audit the audit log itself

            // Update IAuditable temporal and actor properties
            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAtUtc = now;
                    auditable.CreatedByUserId = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.LastModifiedAtUtc = now;
                    auditable.LastModifiedByUserId = userId;
                }
            }

            if (entry.State != EntityState.Added &&
                entry.State != EntityState.Modified &&
                entry.State != EntityState.Deleted)
            {
                continue;
            }

            var auditEntry = new AuditEntryContext(entry)
            {
                TimestampUtc = now,
                ActorUserId = userId,
                TenantId = tenantId,
                IpAddress = ip,
                EntityType = entry.Entity.GetType().Name
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    auditEntry.Action = "Insert";
                    foreach (var prop in entry.Properties)
                    {
                        if (prop.Metadata.IsPrimaryKey())
                        {
                            auditEntry.KeyProperties.Add(prop);
                            continue;
                        }

                        var propName = prop.Metadata.Name;
                        var val = ScrubIfSensitive(propName, prop.CurrentValue);
                        auditEntry.ChangedFields[propName] = new FieldDifference
                        {
                            OriginalValue = null,
                            CurrentValue = val
                        };
                    }
                    break;

                case EntityState.Modified:
                    auditEntry.Action = "Update";
                    foreach (var prop in entry.Properties)
                    {
                        if (prop.Metadata.IsPrimaryKey())
                        {
                            auditEntry.KeyProperties.Add(prop);
                            continue;
                        }

                        if (prop.IsModified)
                        {
                            var originalVal = ScrubIfSensitive(prop.Metadata.Name, prop.OriginalValue);
                            var currentVal = ScrubIfSensitive(prop.Metadata.Name, prop.CurrentValue);

                            // Only record if values actually differ
                            if (!Equals(originalVal, currentVal))
                            {
                                auditEntry.ChangedFields[prop.Metadata.Name] = new FieldDifference
                                {
                                    OriginalValue = originalVal,
                                    CurrentValue = currentVal
                                };
                            }
                        }
                    }
                    break;

                case EntityState.Deleted:
                    auditEntry.Action = "Delete";
                    foreach (var prop in entry.Properties)
                    {
                        if (prop.Metadata.IsPrimaryKey())
                        {
                            auditEntry.KeyProperties.Add(prop);
                            continue;
                        }

                        var propName = prop.Metadata.Name;
                        var val = ScrubIfSensitive(propName, prop.OriginalValue);
                        auditEntry.ChangedFields[propName] = new FieldDifference
                        {
                            OriginalValue = val,
                            CurrentValue = null
                        };
                    }
                    break;
            }

            _pendingAuditEntries.Add(auditEntry);
        }
    }

    private void AfterSaveChanges(DbContext? context)
    {
        if (context == null || _pendingAuditEntries == null || _pendingAuditEntries.Count == 0) return;

        var logs = ConvertPendingToLogs(_pendingAuditEntries);
        _pendingAuditEntries = null;

        if (logs.Count > 0)
        {
            context.Set<AuditChangeLog>().AddRange(logs);
            context.SaveChanges();
        }
    }

    private async Task AfterSaveChangesAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context == null || _pendingAuditEntries == null || _pendingAuditEntries.Count == 0) return;

        var logs = ConvertPendingToLogs(_pendingAuditEntries);
        _pendingAuditEntries = null;

        if (logs.Count > 0)
        {
            await context.Set<AuditChangeLog>().AddRangeAsync(logs, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private List<AuditChangeLog> ConvertPendingToLogs(List<AuditEntryContext> pendingEntries)
    {
        var logs = new List<AuditChangeLog>();
        foreach (var pending in pendingEntries)
        {
            // If it was an update with zero changed fields, skip saving
            if (pending.Action == "Update" && pending.ChangedFields.Count == 0)
                continue;

            var pk = pending.GetPrimaryKeyValue();
            var log = new AuditChangeLog
            {
                Id = Guid.NewGuid(),
                TimestampUtc = pending.TimestampUtc,
                EntityType = pending.EntityType,
                EntityPk = pk,
                Action = pending.Action,
                ActorUserId = pending.ActorUserId,
                TenantId = pending.TenantId,
                IpAddress = pending.IpAddress,
                ChangedFieldsJson = JsonSerializer.Serialize(pending.ChangedFields)
            };

            logs.Add(log);
        }
        return logs;
    }

    public static object? ScrubIfSensitive(string propertyName, object? value)
    {
        if (value == null) return null;

        foreach (var sensitive in SensitiveFieldSubstrings)
        {
            if (propertyName.Contains(sensitive, StringComparison.OrdinalIgnoreCase))
            {
                return "[REDACTED]";
            }
        }

        return value;
    }

    private class AuditEntryContext
    {
        public EntityEntry Entry { get; }
        public DateTime TimestampUtc { get; set; }
        public Guid? ActorUserId { get; set; }
        public Guid? TenantId { get; set; }
        public string? IpAddress { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public List<PropertyEntry> KeyProperties { get; } = new();
        public Dictionary<string, FieldDifference> ChangedFields { get; } = new();

        public AuditEntryContext(EntityEntry entry)
        {
            Entry = entry;
        }

        public string GetPrimaryKeyValue()
        {
            if (KeyProperties.Count == 1)
            {
                return KeyProperties[0].CurrentValue?.ToString() ?? "0";
            }
            if (KeyProperties.Count > 1)
            {
                return string.Join(",", KeyProperties.Select(p => $"{p.Metadata.Name}:{p.CurrentValue}"));
            }

            // Fallback to entry primary key query
            var keyName = Entry.Metadata.FindPrimaryKey()?.Properties.Select(p => p.Name).FirstOrDefault();
            if (keyName != null)
            {
                var val = Entry.Property(keyName).CurrentValue;
                return val?.ToString() ?? "0";
            }

            return "UNKNOWN";
        }
    }
}
