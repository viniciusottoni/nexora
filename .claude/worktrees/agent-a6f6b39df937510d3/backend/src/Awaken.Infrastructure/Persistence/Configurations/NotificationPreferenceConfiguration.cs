using Awaken.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(np => np.Id);

        builder.Property(np => np.UserId).IsRequired();

        builder.Property(np => np.PushEnabled).IsRequired();

        builder.Property(np => np.PushToken)
            .HasMaxLength(512);

        builder.Property(np => np.PermissionStatus)
            .IsRequired()
            .HasMaxLength(32);

        // US-092: campos de agendamento e rate-limiting.
        builder.Property(np => np.PreferredReminderTime);

        builder.Property(np => np.Timezone).HasMaxLength(100).IsRequired(false);

        builder.Property(np => np.LastNotificationSentAt);

        builder.Property(np => np.DailyNotificationCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(np => np.LastNotificationCountResetDate);

        // US-124: controle de frequencia para comunicacao de reativacao.
        builder.Property(np => np.LastReactivationNotificationSentAt);

        builder.HasIndex(np => np.UserId).IsUnique();
    }
}
