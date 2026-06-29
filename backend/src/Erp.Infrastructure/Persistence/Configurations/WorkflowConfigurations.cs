using Erp.Domain.Events;
using Erp.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class StatusTypeConfiguration : IEntityTypeConfiguration<StatusType>
{
    public void Configure(EntityTypeBuilder<StatusType> builder)
    {
        builder.ToTable("status_types", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.WorkspaceId, x.Code }).IsUnique().HasFilter("is_deleted = false");
    }
}

public sealed class StatusConfiguration : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> builder)
    {
        builder.ToTable("statuses", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(30);
        builder.HasOne<StatusType>().WithMany().HasForeignKey(x => x.StatusTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.WorkspaceId, x.StatusTypeId });
    }
}

public sealed class EventStatusConfiguration : IEntityTypeConfiguration<EventStatus>
{
    public void Configure(EntityTypeBuilder<EventStatus> builder)
    {
        builder.ToTable("event_statuses", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note);
        builder.HasOne<Event>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Status>().WithMany().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EventId, x.IsCurrent });
    }
}
