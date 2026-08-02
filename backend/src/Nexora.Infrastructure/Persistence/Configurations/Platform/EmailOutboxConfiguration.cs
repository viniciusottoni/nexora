using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class EmailOutboxConfiguration : IEntityTypeConfiguration<EmailOutbox>
{
    public void Configure(EntityTypeBuilder<EmailOutbox> builder)
    {
        builder.ToTable("email_outbox");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.Recipient).HasColumnName("recipient").HasColumnType("citext").IsRequired();
        builder.Property(e => e.Template).HasColumnName("template").HasMaxLength(64).IsRequired();
        builder.Property(e => e.PayloadEncrypted).HasColumnName("payload_encrypted").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(16).HasDefaultValue("PENDING");
        builder.Property(e => e.Attempts).HasColumnName("attempts").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(e => e.LastError).HasColumnName("last_error");
        builder.Property(e => e.NextAttemptAt).HasColumnName("next_attempt_at").HasColumnType("timestamptz");
        builder.Property(e => e.SentAt).HasColumnName("sent_at").HasColumnType("timestamptz");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.TenantId, e.Status, e.NextAttemptAt }).HasDatabaseName("idx_email_outbox_pending");
    }
}
