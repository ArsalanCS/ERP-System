using Erp.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Slug).HasMaxLength(80).IsRequired();
        builder.Property(w => w.LegalName).HasMaxLength(250);
        builder.Property(w => w.DefaultLanguage).HasMaxLength(8).IsRequired();
        builder.Property(w => w.TimeZone).HasMaxLength(64).IsRequired();
        builder.Property(w => w.BaseCurrency).HasMaxLength(3).IsRequired();
        builder.Property(w => w.Country).HasMaxLength(2);
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);

        // Slug must be globally unique — it resolves the workspace at login.
        builder.HasIndex(w => w.Slug).IsUnique();
        builder.HasIndex(w => w.IsDeleted);
    }
}
