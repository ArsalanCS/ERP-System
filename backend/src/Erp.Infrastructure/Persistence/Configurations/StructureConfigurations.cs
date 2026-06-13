using Erp.Domain.Structure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Code).HasMaxLength(60).IsRequired();
        builder.Property(o => o.LegalName).HasMaxLength(250);
        builder.Property(o => o.OrganizationType).HasMaxLength(80);
        builder.Property(o => o.CommercialRegistrationNumber).HasMaxLength(50);
        builder.Property(o => o.TaxNumber).HasMaxLength(50);
        builder.Property(o => o.Country).HasMaxLength(2);
        builder.Property(o => o.City).HasMaxLength(120);
        builder.Property(o => o.BaseCurrency).HasMaxLength(3);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(o => new { o.WorkspaceId, o.Code }).IsUnique().HasFilter("is_deleted = false");
    }
}

public sealed class ClusterConfiguration : IEntityTypeConfiguration<Cluster>
{
    public void Configure(EntityTypeBuilder<Cluster> builder)
    {
        builder.ToTable("clusters");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Code).HasMaxLength(60).IsRequired();
        builder.Property(c => c.Type).HasMaxLength(60).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Location).HasMaxLength(200);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(c => new { c.WorkspaceId, c.Code }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(c => c.OrganizationId);
        builder.HasOne<Organization>().WithMany().HasForeignKey(c => c.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Code).HasMaxLength(60).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(d => new { d.WorkspaceId, d.Code }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne<Organization>().WithMany().HasForeignKey(d => d.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Code).HasMaxLength(60).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(t => new { t.WorkspaceId, t.Code }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne<Department>().WithMany().HasForeignKey(t => t.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
