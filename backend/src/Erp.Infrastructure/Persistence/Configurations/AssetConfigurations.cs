using Erp.Domain.Assets;
using Erp.Domain.Events;
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

public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("notes", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Body).IsRequired();
        builder.HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AssetId).IsUnique();
    }
}

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
