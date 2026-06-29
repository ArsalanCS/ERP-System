using Erp.Domain.Mail;
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

public sealed class SendMailConfiguration : IEntityTypeConfiguration<SendMail>
{
    public void Configure(EntityTypeBuilder<SendMail> builder)
    {
        builder.ToTable("send_mails", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MailTemplateId).HasColumnName("mail_template_id");
        builder.Property(x => x.TemplateCode).HasColumnName("template_code").HasMaxLength(100);
        builder.Property(x => x.Subject).HasMaxLength(300).IsRequired();
        builder.Property(x => x.BodyHtml).HasColumnName("body_html").IsRequired();
        builder.Property(x => x.BodyText).HasColumnName("body_text");
        builder.Property(x => x.TemplateDataJson).HasColumnName("template_data_json").HasColumnType("jsonb");
        builder.Property(x => x.Status).HasColumnName("send_status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ScheduledAt).HasColumnName("scheduled_at");
        builder.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(x => x.SentAt).HasColumnName("sent_at");
        builder.Property(x => x.RetryCount).HasColumnName("retry_count");
        builder.Property(x => x.MaxRetries).HasColumnName("max_retries");
        builder.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(2000);
        // Dispatcher polling index: pending work ordered by readiness.
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
    }
}

public sealed class SendMailRecipientConfiguration : IEntityTypeConfiguration<SendMailRecipient>
{
    public void Configure(EntityTypeBuilder<SendMailRecipient> builder)
    {
        builder.ToTable("send_mail_recipients", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Address).HasMaxLength(320).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200);
        builder.Property(x => x.Kind).HasColumnName("recipient_type").HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.HasOne<SendMail>().WithMany().HasForeignKey(x => x.SendMailId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.SendMailId);
    }
}

public sealed class SendMailAttemptConfiguration : IEntityTypeConfiguration<SendMailAttempt>
{
    public void Configure(EntityTypeBuilder<SendMailAttempt> builder)
    {
        builder.ToTable("send_mail_attempts", "bpm");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AttemptNo).HasColumnName("attempt_no");
        builder.Property(x => x.ProviderResponse).HasColumnName("provider_response");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(x => x.AttemptedAt).HasColumnName("attempted_at");
        builder.HasOne<SendMail>().WithMany().HasForeignKey(x => x.SendMailId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.SendMailId);
    }
}
