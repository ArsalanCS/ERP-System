using System.Linq.Expressions;
using Erp.Application.Abstractions;
using Erp.Domain.Auditing;
using Erp.Domain.Assets;
using Erp.Domain.Authorization;
using Erp.Domain.Common;
using Erp.Domain.Events;
using Erp.Domain.Tasks;
using Erp.Domain.Identity;
using Erp.Domain.Mailing;
using Erp.Domain.Structure;
using Erp.Domain.Tenancy;
using Erp.Domain.Statuses;
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
    public long CurrentWorkspaceId => tenant.WorkspaceId ?? 0;

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
    public DbSet<StructureNode> StructureNodes => Set<StructureNode>();
    public DbSet<Employee> Employees => Set<Employee>();

    // Event / Asset / Task architecture (bpm schema).
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<TaskEvent> TaskEvents => Set<TaskEvent>();
    public DbSet<EventActivity> EventActivities => Set<EventActivity>();
    public DbSet<EventDependency> EventDependencies => Set<EventDependency>();
    public DbSet<EventDailyReport> EventDailyReports => Set<EventDailyReport>();
    public DbSet<TaskSettings> TaskSettings => Set<TaskSettings>();
    public DbSet<StatusType> StatusTypes => Set<StatusType>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<EventStatus> EventStatuses => Set<EventStatus>();
    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<EventAsset> EventAssets => Set<EventAsset>();

    public DbSet<MailTemplate> MailTemplates => Set<MailTemplate>();
    public DbSet<SendMail> SendMails => Set<SendMail>();
    public DbSet<SendMailRecipient> SendMailRecipients => Set<SendMailRecipient>();
    public DbSet<SendMailAttempt> SendMailAttempts => Set<SendMailAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);

        // Keyless Row Models for the bpm.fn_task_* DB functions: queried only via
        // FromSqlRaw and mapped to DTOs in TaskReadRepository, so they are excluded
        // from migrations (no backing table — the function supplies the rows).
        ConfigureRowModel<ReadModels.TaskSummaryRow>(modelBuilder, "task_summary_row");
        ConfigureRowModel<ReadModels.TaskBucketRow>(modelBuilder, "task_bucket_row");
        ConfigureRowModel<ReadModels.TaskAssigneeLoadRow>(modelBuilder, "task_assignee_load_row");
        ConfigureRowModel<ReadModels.TaskTrendRow>(modelBuilder, "task_trend_row");
        ConfigureRowModel<ReadModels.TaskRecentActivityRow>(modelBuilder, "task_recent_activity_row");
        ConfigureRowModel<ReadModels.TaskGanttRow>(modelBuilder, "task_gantt_row");
        ConfigureRowModel<ReadModels.TaskDailyReportRow>(modelBuilder, "task_daily_report_row");
        ConfigureRowModel<ReadModels.TaskListItemRow>(modelBuilder, "task_list_item_row");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(BaseEntity).IsAssignableFrom(clrType))
            {
                continue;
            }

            // BigInt keys are assigned client-side by IdGenerator (known pre-save),
            // so the database must not try to generate them.
            modelBuilder.Entity(clrType)
                .Property(nameof(BaseEntity.Id))
                .ValueGeneratedNever();

            // Append-only audit rows live on a partitioned table that cannot
            // RETURN the xmin system column on insert, and are never updated —
            // so they don't carry a concurrency token.
            if (clrType == typeof(AuditLog))
            {
                modelBuilder.Entity(clrType).Ignore(nameof(BaseEntity.Version));
            }
            else
            {
                // xmin optimistic-concurrency token for every other entity.
                modelBuilder.Entity(clrType)
                    .Property(nameof(BaseEntity.Version))
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

    private static void ConfigureRowModel<T>(ModelBuilder modelBuilder, string name) where T : class =>
        modelBuilder.Entity<T>().HasNoKey().ToTable(name, t => t.ExcludeFromMigrations());

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
