using Erp.Domain.Events;
using Erp.Domain.Tasks;
using Erp.Domain.Statuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

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
