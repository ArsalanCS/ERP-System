using Erp.Domain.Mailing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class MailTemplateConfiguration : IEntityTypeConfiguration<MailTemplate>
{
    public void Configure(EntityTypeBuilder<MailTemplate> builder)
    {
        builder.ToTable("mail_templates", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WorkspaceId); // nullable: null = global default
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SubjectTemplate).HasColumnName("subject_template").HasMaxLength(300).IsRequired();
        builder.Property(x => x.BodyHtmlTemplate).HasColumnName("body_html_template").IsRequired();
        builder.Property(x => x.BodyTextTemplate).HasColumnName("body_text_template");
        builder.Ignore(x => x.IsGlobal);
        // One template per (workspace, code); global defaults share workspace_id NULL.
        builder.HasIndex(x => new { x.WorkspaceId, x.Code }).IsUnique().HasFilter("is_deleted = false");
    }
}
