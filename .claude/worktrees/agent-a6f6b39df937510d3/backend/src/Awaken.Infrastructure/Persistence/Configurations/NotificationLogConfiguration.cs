using Awaken.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.UserId).IsRequired();

        builder.Property(l => l.NotificationType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(l => l.DecisionStatus)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(l => l.DecisionReason)
            .HasMaxLength(64);

        builder.Property(l => l.AttemptedAtUtc).IsRequired();

        builder.HasIndex(l => new { l.UserId, l.AttemptedAtUtc });
    }
}
