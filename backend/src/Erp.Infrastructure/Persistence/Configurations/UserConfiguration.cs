using Erp.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512);
        builder.Property(u => u.SecurityStamp).HasMaxLength(64).IsRequired();

        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Mobile).HasMaxLength(32);
        builder.Property(u => u.PreferredLanguage).HasMaxLength(8).IsRequired();
        builder.Property(u => u.TimeZone).HasMaxLength(64).IsRequired();
        builder.Property(u => u.JobTitle).HasMaxLength(150);
        builder.Property(u => u.AvatarUrl).HasMaxLength(1024);
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.TwoFactorSecret).HasMaxLength(256);

        // Per-workspace email uniqueness (active rows only).
        builder.HasIndex(u => new { u.WorkspaceId, u.NormalizedEmail })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(u => u.WorkspaceId);

        builder.HasOne<Domain.Tenancy.Workspace>()
            .WithMany()
            .HasForeignKey(u => u.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
