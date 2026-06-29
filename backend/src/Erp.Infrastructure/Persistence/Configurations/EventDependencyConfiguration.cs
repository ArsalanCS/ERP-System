using Erp.Domain.Events;
using Erp.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

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
