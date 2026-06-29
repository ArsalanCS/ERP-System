using Erp.Domain.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

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
