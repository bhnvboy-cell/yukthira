using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Data;

public class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenant;

    public TenantSaveChangesInterceptor(ITenantContext tenant) { _tenant = tenant; }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyTenant(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTenant(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyTenant(DbContext? context)
    {
        if (context == null || _tenant.TenantId == Guid.Empty) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added) continue;

            var prop = entry.Entity.GetType().GetProperty("TenantId");
            if (prop == null || prop.PropertyType != typeof(Guid)) continue;

            var current = (Guid?)prop.GetValue(entry.Entity);
            if (current == Guid.Empty || current == null)
                prop.SetValue(entry.Entity, _tenant.TenantId);
        }
    }
}