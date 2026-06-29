using Erp.Application.Abstractions;
using Erp.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Erp.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps audit metadata (created/updated by + at) on save, and enforces — as a
/// second line of defense beyond RLS and query filters — that no tenant-owned
/// row is written outside the caller's workspace scope (CLAUDE.md §4.1).
/// </summary>
public sealed class AuditSaveChangesInterceptor(IClock clock, ICurrentUser currentUser, ITenantContext tenant)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;

        var now = clock.UtcNow;
        var userId = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.InsertedDate = now;
                    entry.Entity.InsertedBy ??= userId;
                    GuardTenant(entry);
                    break;
                case EntityState.Modified:
                    entry.Entity.ChangedDate = now;
                    entry.Entity.ChangedBy = userId;
                    GuardTenant(entry);
                    break;
            }
        }
    }

    private void GuardTenant(EntityEntry<BaseEntity> entry)
    {
        if (entry.Entity is not ITenantOwned owned) return;
        if (tenant.IsPlatformAdmin || !tenant.HasScope) return;

        if (owned.WorkspaceId != tenant.WorkspaceId)
        {
            throw new InvalidOperationException(
                $"Cross-tenant write blocked: entity {entry.Entity.GetType().Name} has workspace " +
                $"{owned.WorkspaceId} but the current scope is {tenant.WorkspaceId}.");
        }
    }
}
