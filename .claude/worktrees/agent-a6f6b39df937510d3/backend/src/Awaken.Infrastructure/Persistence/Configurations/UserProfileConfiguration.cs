using Awaken.Domain.Entities.Onboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.Goal)
            .IsRequired(false)
            .HasMaxLength(32);

        builder.Property(p => p.ExperienceLevel)
            .IsRequired(false)
            .HasMaxLength(32);

        builder.Property(p => p.EffectiveExperienceLevel)
            .IsRequired(false)
            .HasMaxLength(32);

        builder.Property(p => p.Age).IsRequired(false);

        builder.Property(p => p.HeightCm)
            .IsRequired(false)
            .HasPrecision(5, 2);

        builder.Property(p => p.WeightKg)
            .IsRequired(false)
            .HasPrecision(6, 2);

        builder.Property(p => p.BiologicalSex)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(p => p.TrainingDuration)
            .IsRequired(false)
            .HasMaxLength(32);

        builder.Property(p => p.TrainingLocation)
            .IsRequired(false)
            .HasMaxLength(32);

        builder.Property(p => p.EquipmentAvailable)
            .IsRequired(false)
            .HasColumnType("text[]");

        builder.Property(p => p.AvailableMinutesPerWorkout)
            .IsRequired(false);

        builder.Property(p => p.AvailableDaysPerWeek)
            .IsRequired(false);

        builder.Property(p => p.BodyType)
            .IsRequired(false)
            .HasMaxLength(32);

        builder.Property(p => p.PhysicalLimitations)
            .IsRequired(false)
            .HasColumnType("text[]");

        builder.Property(p => p.PhysicalPains)
            .IsRequired(false)
            .HasColumnType("text[]");

        builder.Property(p => p.TrainingPreferences)
            .IsRequired(false)
            .HasColumnType("text[]");
    }
}
