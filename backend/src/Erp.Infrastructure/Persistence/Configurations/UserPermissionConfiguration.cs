using Erp.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("user_permissions");
        builder.HasKey(up => up.Id);
        builder.Property(up => up.Effect).HasConversion<string>().HasMaxLength(10);
        builder.Property(up => up.Scope).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(up => new { up.UserId, up.PermissionId }).IsUnique();
        builder.HasOne<Permission>().WithMany().HasForeignKey(up => up.PermissionId).OnDelete(DeleteBehavior.Restrict);
    }
}
