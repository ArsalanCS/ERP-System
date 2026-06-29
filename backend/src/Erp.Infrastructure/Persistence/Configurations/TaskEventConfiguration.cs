using Erp.Domain.Tasks;
using Erp.Domain.Statuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

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
