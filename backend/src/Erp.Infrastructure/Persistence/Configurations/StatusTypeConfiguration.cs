using Erp.Domain.Tasks;
using Erp.Domain.Statuses;
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
