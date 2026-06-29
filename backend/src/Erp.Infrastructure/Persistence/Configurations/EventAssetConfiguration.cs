using Erp.Domain.Assets;
using Erp.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class EventAssetConfiguration : IEntityTypeConfiguration<EventAsset>
{
    public void Configure(EntityTypeBuilder<EventAsset> builder)
    {
        builder.ToTable("event_assets", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RelationType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description);
        builder.HasOne<Event>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.WorkspaceId, x.EventId });
        builder.HasIndex(x => new { x.EventId, x.AssetId, x.RelationType }).IsUnique().HasFilter("is_deleted = false");
    }
}
