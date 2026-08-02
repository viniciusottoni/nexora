using Awaken.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(512);

        builder.Property(u => u.DisplayName)
            .HasMaxLength(100);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(2048);

        // US-234: chave de avatar interno de catalogo selecionado manualmente.
        builder.Property(u => u.SelectedAvatarKey)
            .HasMaxLength(64);

        builder.Property(u => u.PreferredLanguage)
            .IsRequired()
            .HasMaxLength(8);

        builder.Property(u => u.OnboardingStartedAtUtc).IsRequired(false);
        builder.Property(u => u.OnboardingCompletedAtUtc).IsRequired(false);
        builder.Property(u => u.CurrentOnboardingStep)
            .HasMaxLength(64);

        builder.Property(u => u.Provider)
            .IsRequired()
            .HasMaxLength(16)
            .HasConversion<string>();

        builder.Property(u => u.ProviderUserId)
            .HasMaxLength(256);

        builder.HasIndex(u => new { u.Provider, u.ProviderUserId })
            .IsUnique()
            .HasFilter("\"ProviderUserId\" IS NOT NULL");

        builder.Property(u => u.TrialEndsAt).IsRequired(false);

        builder.Property(u => u.TermsAcceptedAt).IsRequired(false);
        builder.Property(u => u.PrivacyAcceptedAt).IsRequired(false);
        builder.Property(u => u.TermsVersion).HasMaxLength(20).IsRequired(false);
        builder.Property(u => u.PrivacyVersion).HasMaxLength(20).IsRequired(false);
        builder.Property(u => u.ResponsibilityNoticeAcceptedAt).IsRequired(false);
        builder.Property(u => u.ResponsibilityNoticeVersion).HasMaxLength(20).IsRequired(false);

        builder.Property(u => u.Role)
            .HasMaxLength(32)
            .IsRequired(false);
    }
}
