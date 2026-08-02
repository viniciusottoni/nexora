using Awaken.Domain.Entities.Onboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class UserWorkoutPreferenceConfiguration : IEntityTypeConfiguration<UserWorkoutPreference>
{
    public void Configure(EntityTypeBuilder<UserWorkoutPreference> builder)
    {
        builder.ToTable("user_workout_preferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.PreferredTrainingType)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(p => p.PreferredProgramId)
            .IsRequired(false)
            .HasMaxLength(64);
    }
}
