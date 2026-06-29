using Erp.Domain.Assets;
using Erp.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class AssetTypeConfiguration : IEntityTypeConfiguration<AssetType>
{
    public void Configure(EntityTypeBuilder<AssetType> builder)
    {
        builder.ToTable("asset_types", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
