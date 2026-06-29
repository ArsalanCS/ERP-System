using Erp.Domain.Assets;
using Erp.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(300);
        builder.Property(x => x.Code).HasMaxLength(100);
        builder.HasOne<AssetType>().WithMany().HasForeignKey(x => x.AssetTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.WorkspaceId, x.AssetTypeId });
    }
}
