using Awaken.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ActorUserId).IsRequired(false);

        builder.Property(a => a.ActorType)
            .IsRequired()
            .HasMaxLength(16)
            .HasConversion<string>();

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(a => a.ResourceType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(a => a.ResourceId).IsRequired(false);

        builder.Property(a => a.MetadataSafe)
            .HasMaxLength(512);

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(64);

        builder.HasIndex(a => new { a.ActorUserId, a.CreatedAtUtc });
        builder.HasIndex(a => a.Action);
    }
}
