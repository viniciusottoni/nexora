using Awaken.Domain.Entities.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class QuestExerciseConfiguration : IEntityTypeConfiguration<QuestExercise>
{
    public void Configure(EntityTypeBuilder<QuestExercise> builder)
    {
        builder.ToTable("quest_exercises");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.QuestId).IsRequired();

        builder.Property(e => e.Order).IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.ExerciseCatalogProviderId)
            .IsRequired(false)
            .HasMaxLength(128);

        builder.Property(e => e.Sets).IsRequired();
        builder.Property(e => e.RepsMin).IsRequired();
        builder.Property(e => e.RepsMax).IsRequired(false);
        builder.Property(e => e.RestSeconds).IsRequired(false);

        builder.Property(e => e.TargetRpe)
            .IsRequired(false)
            .HasMaxLength(16);

        builder.Property(e => e.VideoUrl)
            .IsRequired(false)
            .HasMaxLength(512);

        builder.Property(e => e.XpReward).IsRequired();
        builder.Property(e => e.StrengthXp).IsRequired();
        builder.Property(e => e.AgilityXp).IsRequired();
        builder.Property(e => e.EnduranceXp).IsRequired();
        builder.Property(e => e.VitalityXp).IsRequired();
        builder.Property(e => e.FocusXp).IsRequired();
        builder.Property(e => e.WisdomXp).IsRequired();

        builder.Property(e => e.CompletedAtUtc).IsRequired(false);
        builder.Property(e => e.XpEarned).IsRequired();

        // US-130: level-ups de atributo ocorridos na conclusão do exercício.
        builder.Property(e => e.StrengthLevelUps).IsRequired().HasDefaultValue(0);
        builder.Property(e => e.AgilityLevelUps).IsRequired().HasDefaultValue(0);
        builder.Property(e => e.EnduranceLevelUps).IsRequired().HasDefaultValue(0);
        builder.Property(e => e.VitalityLevelUps).IsRequired().HasDefaultValue(0);
        builder.Property(e => e.FocusLevelUps).IsRequired().HasDefaultValue(0);
        builder.Property(e => e.WisdomLevelUps).IsRequired().HasDefaultValue(0);

        builder.HasIndex(e => new { e.QuestId, e.Order }).IsUnique();
    }
}
