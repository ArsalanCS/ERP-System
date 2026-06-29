using Erp.Domain.Assets;
using Erp.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.FilePath).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(150);
        builder.HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AssetId).IsUnique();
    }
}
