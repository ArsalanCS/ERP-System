using Erp.Domain.Structure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class StructureNodeConfiguration : IEntityTypeConfiguration<StructureNode>
{
    public void Configure(EntityTypeBuilder<StructureNode> builder)
    {
        builder.ToTable("structure_nodes");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Name).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Code).HasMaxLength(60).IsRequired();
        builder.Property(n => n.Description).HasMaxLength(500);
        builder.Property(n => n.NodeType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(n => new { n.WorkspaceId, n.Code }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(n => n.ParentId);

        // Self-reference for the tree (no cascade — archive, don't hard delete).
        builder.HasOne<StructureNode>().WithMany().HasForeignKey(n => n.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}
