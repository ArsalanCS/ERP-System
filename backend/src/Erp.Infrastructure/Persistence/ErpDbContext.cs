using System.Linq.Expressions;
using Erp.Application.Abstractions;
using Erp.Domain.Auditing;
using Erp.Domain.Authorization;
using Erp.Domain.Common;
using Erp.Domain.Identity;
using Erp.Domain.Structure;
using Erp.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence;

/// <summary>
/// Root EF Core context. Applies the cross-cutting persistence conventions every
/// entity inherits: UUID-v7 keys, audit columns, xmin concurrency token, and the
/// tenant + soft-delete global query filters (CLAUDE.md §4.1, §4.9).
/// </summary>
public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options, ITenantContext tenant)
    : DbContext(options)
{
    /// <summary>Workspace the queries are scoped to (read by the global query filter).</summary>
    public Guid CurrentWorkspaceId => tenant.WorkspaceId ?? Guid.Empty;

    /// <summary>Platform super admin bypasses the tenant filter (cross-tenant reads).</summary>
    public bool BypassTenantFilter => tenant.IsPlatformAdmin;

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WorkspaceSecurityPolicy> WorkspaceSecurityPolicies => Set<WorkspaceSecurityPolicy>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Cluster> Clusters => Set<Cluster>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Team> Teams => Set<Team>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(Entity).IsAssignableFrom(clrType))
            {
                continue;
            }

            // Append-only audit rows live on a partitioned table that cannot
            // RETURN the xmin system column on insert, and are never updated —
            // so they don't carry a concurrency token.
            if (clrType == typeof(AuditLog))
            {
                modelBuilder.Entity(clrType).Ignore(nameof(Entity.Version));
            }
            else
            {
                // xmin optimistic-concurrency token for every other entity.
                modelBuilder.Entity(clrType)
                    .Property(nameof(Entity.Version))
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            }

            var filter = BuildGlobalFilter(clrType);
            if (filter is not null)
            {
                modelBuilder.Entity(clrType).HasQueryFilter(filter);
            }
        }
    }

    /// <summary>
    /// Builds <c>e =&gt; !e.IsDeleted &amp;&amp; (BypassTenantFilter || e.WorkspaceId == CurrentWorkspaceId)</c>,
    /// including only the clauses the entity supports. Referencing context
    /// instance members makes EF re-evaluate the scope per query.
    /// </summary>
    private LambdaExpression? BuildGlobalFilter(Type clrType)
    {
        var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(clrType);
        var isTenantOwned = typeof(ITenantOwned).IsAssignableFrom(clrType);
        if (!isSoftDeletable && !isTenantOwned)
        {
            return null;
        }

        var parameter = Expression.Parameter(clrType, "e");
        Expression? body = null;

        if (isSoftDeletable)
        {
            var isDeleted = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            body = Expression.Not(isDeleted);
        }

        if (isTenantOwned)
        {
            var contextConstant = Expression.Constant(this);
            var bypass = Expression.Property(contextConstant, nameof(BypassTenantFilter));
            var currentWorkspace = Expression.Property(contextConstant, nameof(CurrentWorkspaceId));
            var workspace = Expression.Property(parameter, nameof(ITenantOwned.WorkspaceId));
            var tenantClause = Expression.OrElse(bypass, Expression.Equal(workspace, currentWorkspace));
            body = body is null ? tenantClause : Expression.AndAlso(body, tenantClause);
        }

        return Expression.Lambda(body!, parameter);
    }
}
