using Erp.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="AuditLog"/> for reads/inserts but excludes it from EF
/// migrations — the table is created by raw SQL as a monthly-partitioned,
/// append-only table (CLAUDE.md §4.3). Column names here must match that SQL.
/// </summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", t => t.ExcludeFromMigrations());
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CorrelationId).HasMaxLength(64);
        builder.Property(a => a.Module).HasMaxLength(50);
        builder.Property(a => a.ResourceType).HasMaxLength(80);
        builder.Property(a => a.ResourceId).HasMaxLength(80);
        builder.Property(a => a.Action).HasMaxLength(40);
        builder.Property(a => a.ActorDisplayName).HasMaxLength(256);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
        builder.Property(a => a.UserAgent).HasMaxLength(512);
        builder.Property(a => a.Reason).HasMaxLength(1000);
        builder.Property(a => a.OldValues).HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnType("jsonb");
        builder.Property(a => a.Result).HasConversion<string>().HasMaxLength(10);
        builder.Property(a => a.Source).HasConversion<string>().HasMaxLength(20);
    }
}
