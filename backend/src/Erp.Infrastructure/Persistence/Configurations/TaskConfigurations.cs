using Erp.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskStatus = Erp.Domain.Tasks.TaskStatus; // disambiguate from System.Threading.Tasks.TaskStatus

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TaskNumber).HasMaxLength(40).IsRequired();
        builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(4000);
        builder.Property(t => t.EventType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.EstimatedHours).HasPrecision(9, 2);
        builder.Property(t => t.ActualHours).HasPrecision(9, 2);

        builder.HasIndex(t => new { t.WorkspaceId, t.TaskNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(t => new { t.WorkspaceId, t.StatusId });
        builder.HasIndex(t => new { t.WorkspaceId, t.AssigneeId });
        builder.HasIndex(t => new { t.WorkspaceId, t.DueDate });
        builder.HasIndex(t => t.ParentTaskId);

        // Workflow links (within the Tasks module). Restrict — statuses/types are
        // soft-managed, never hard-deleted out from under a task.
        builder.HasOne<TaskStatus>().WithMany().HasForeignKey(t => t.StatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaskStatusType>().WithMany().HasForeignKey(t => t.StatusTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaskItem>().WithMany().HasForeignKey(t => t.ParentTaskId).OnDelete(DeleteBehavior.Restrict);
        // AssigneeId / ReporterId reference users but are kept FK-free here: users
        // are soft-deleted (never removed), so app-level integrity is sufficient.
    }
}

public sealed class TaskStatusTypeConfiguration : IEntityTypeConfiguration<TaskStatusType>
{
    public void Configure(EntityTypeBuilder<TaskStatusType> builder)
    {
        builder.ToTable("task_status_types");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(120).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);

        builder.HasIndex(t => new { t.WorkspaceId, t.Name }).IsUnique().HasFilter("is_deleted = false");
    }
}

public sealed class TaskStatusConfiguration : IEntityTypeConfiguration<TaskStatus>
{
    public void Configure(EntityTypeBuilder<TaskStatus> builder)
    {
        builder.ToTable("task_statuses");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(120).IsRequired();
        builder.Property(s => s.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Color).HasMaxLength(20);

        builder.HasIndex(s => new { s.WorkspaceId, s.StatusTypeId });

        builder.HasOne<TaskStatusType>().WithMany().HasForeignKey(s => s.StatusTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TaskActivityConfiguration : IEntityTypeConfiguration<TaskActivity>
{
    public void Configure(EntityTypeBuilder<TaskActivity> builder)
    {
        builder.ToTable("task_activities");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Message).HasMaxLength(1000).IsRequired();

        builder.HasIndex(a => new { a.WorkspaceId, a.TaskId });

        builder.HasOne<TaskItem>().WithMany().HasForeignKey(a => a.TaskId).OnDelete(DeleteBehavior.Restrict);
    }
}
