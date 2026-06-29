using Erp.Domain.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

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
