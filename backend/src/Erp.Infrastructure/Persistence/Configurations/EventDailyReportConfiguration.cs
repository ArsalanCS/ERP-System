using Erp.Domain.Events;
using Erp.Domain.Tasks;
using Erp.Domain.Statuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

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
