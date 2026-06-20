using Erp.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeNumber).HasMaxLength(40);
        builder.Property(e => e.JobTitle).HasMaxLength(150);
        builder.Property(e => e.Mobile).HasMaxLength(32);

        // One employee record per user (active rows).
        builder.HasIndex(e => new { e.WorkspaceId, e.UserId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(e => e.PlacementNodeId);

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Domain.Structure.StructureNode>().WithMany()
            .HasForeignKey(e => e.PlacementNodeId).OnDelete(DeleteBehavior.SetNull);
    }
}
