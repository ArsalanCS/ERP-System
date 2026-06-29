using Erp.Domain.Tasks;
using Erp.Domain.Statuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

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
