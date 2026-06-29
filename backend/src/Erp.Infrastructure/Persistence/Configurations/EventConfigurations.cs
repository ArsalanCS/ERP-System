using Erp.Domain.Events;
using Erp.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class EventTypeConfiguration : IEntityTypeConfiguration<EventType>
{
    public void Configure(EntityTypeBuilder<EventType> builder)
    {
        builder.ToTable("event_types", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events", "bpm");
        builder.HasKey(x => x.Id);
        builder.HasOne<EventType>().WithMany().HasForeignKey(x => x.EventTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.WorkspaceId, x.EventTypeId });
    }
}

public sealed class TaskEventConfiguration : IEntityTypeConfiguration<TaskEvent>
{
    public void Configure(EntityTypeBuilder<TaskEvent> builder)
    {
        builder.ToTable("task_events", "bpm");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReferenceNo).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description);
        builder.Property(x => x.EstimatedTime).HasColumnType("numeric(9,2)");
        builder.Property(x => x.ActualTime).HasColumnType("numeric(9,2)");

        builder.HasOne<Event>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.EventId).IsUnique();
        builder.HasIndex(x => new { x.WorkspaceId, x.ReferenceNo }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(x => x.AssigneeId);
        builder.HasIndex(x => x.ParentEventId);
        builder.HasIndex(x => x.DueAt);
    }
}

public sealed class EventDependencyConfiguration : IEntityTypeConfiguration<EventDependency>
{
    public void Configure(EntityTypeBuilder<EventDependency> builder)
    {
        builder.ToTable("event_dependencies", "bpm");
        builder.HasKey(x => x.Id);
        builder.HasOne<Event>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>().WithMany().HasForeignKey(x => x.DependsOnEventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EventId, x.DependsOnEventId }).IsUnique().HasFilter("is_deleted = false");
    }
}

public sealed class EventActivityConfiguration : IEntityTypeConfiguration<EventActivity>
{
    public void Configure(EntityTypeBuilder<EventActivity> builder)
    {
        builder.ToTable("event_activities", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(500).IsRequired();
        builder.HasOne<Event>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EventId, x.OccurredAt });
    }
}

public sealed class TaskSettingsConfiguration : IEntityTypeConfiguration<TaskSettings>
{
    public void Configure(EntityTypeBuilder<TaskSettings> builder)
    {
        builder.ToTable("task_settings", "bpm");
        builder.HasKey(x => x.Id);
        // One settings row per workspace.
        builder.HasIndex(x => x.WorkspaceId).IsUnique().HasFilter("is_deleted = false");
    }
}

public sealed class EventDailyReportConfiguration : IEntityTypeConfiguration<EventDailyReport>
{
    public void Configure(EntityTypeBuilder<EventDailyReport> builder)
    {
        builder.ToTable("event_daily_reports", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.EstimatedTime).HasColumnType("numeric(9,2)");
        builder.Property(x => x.ActualTime).HasColumnType("numeric(9,2)");
        builder.Property(x => x.RemainingTime).HasColumnType("numeric(9,2)");
        builder.Property(x => x.StatusId).HasColumnName("status_id");
        builder.HasOne<Event>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Status>().WithMany().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EventId, x.ReportDate });
        // One report per author per day per event (unless settings allow multiple — enforced in service when configured).
        builder.HasIndex(x => new { x.EventId, x.ReportDate, x.UserId }).IsUnique().HasFilter("is_deleted = false");
    }
}
