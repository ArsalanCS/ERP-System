using Erp.Domain.Mailing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

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
