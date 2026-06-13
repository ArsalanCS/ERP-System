using Erp.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Module).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Resource).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Action).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Code).HasMaxLength(60).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.Color).HasMaxLength(20);
        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(r => new { r.WorkspaceId, r.Code }).IsUnique().HasFilter("is_deleted = false");

        builder.HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(Role.Permissions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(rp => rp.Id);
        builder.Property(rp => rp.Scope).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
        builder.HasOne<Permission>().WithMany().HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.HasKey(ur => ur.Id);
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId, ur.ClusterId }).IsUnique();
        builder.HasOne<Role>().WithMany().HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

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
