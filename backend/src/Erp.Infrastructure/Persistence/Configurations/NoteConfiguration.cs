using Erp.Domain.Assets;
using Erp.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

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
