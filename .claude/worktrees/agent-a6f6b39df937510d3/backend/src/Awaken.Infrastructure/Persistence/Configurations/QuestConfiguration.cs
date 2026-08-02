using Awaken.Domain.Entities.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class QuestConfiguration : IEntityTypeConfiguration<Quest>
{
    public void Configure(EntityTypeBuilder<Quest> builder)
    {
        builder.ToTable("quests");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.UserId).IsRequired();

        builder.Property(q => q.Type)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(q => q.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(q => q.Language)
            .IsRequired()
            .HasMaxLength(8);

        builder.Property(q => q.IdempotencyKey)
            .IsRequired(false)
            .HasMaxLength(128);

        builder.Property(q => q.QuestDateUtc).IsRequired();

        builder.Property(q => q.RegenerationCount).IsRequired();

        builder.Property(q => q.GenerationReason)
            .IsRequired(false)
            .HasMaxLength(32);

        builder.Property(q => q.GenerationMethod)
            .IsRequired(false)
            .HasMaxLength(64);

        builder.Property(q => q.ProfileSnapshotJson).IsRequired(false);
        builder.Property(q => q.AppliedFiltersJson).IsRequired(false);

        builder.Property(q => q.TrainingType)
            .IsRequired()
            .HasMaxLength(32)
            .HasDefaultValue("personalized_individual");

        builder.Property(q => q.ProgramId)
            .IsRequired(false)
            .HasMaxLength(64);

        // US-238: dia do programa resolvido pela rotação cíclica (auditoria + idempotência do dia).
        builder.Property(q => q.ResolvedProgramKey)
            .IsRequired(false)
            .HasMaxLength(64);

        builder.Property(q => q.ResolvedDayKey)
            .IsRequired(false)
            .HasMaxLength(8);

        builder.Property(q => q.ResolvedDayIndex)
            .IsRequired(false);

        builder.Property(q => q.SplitMapVersion)
            .IsRequired(false)
            .HasMaxLength(16);

        // US-240: snapshot do DailyWorkoutBlueprint usado nesta geração (auditoria, RN-008/US-049).
        builder.Property(q => q.DailyWorkoutBlueprintJson).IsRequired(false);

        // US-242: orçamento de tempo determinístico usado nesta geração (auditoria, RN-007).
        builder.Property(q => q.EstimatedDurationSeconds).IsRequired(false);
        builder.Property(q => q.TimeBudgetSeconds).IsRequired(false);
        builder.Property(q => q.TimeAdjustmentApplied).IsRequired(false).HasMaxLength(32);
        builder.Property(q => q.WorkoutTimeModelVersion).IsRequired(false).HasMaxLength(16);

        // US-241: snapshot do WeeklyProgressionPlan usado nesta geração (auditoria).
        builder.Property(q => q.WeeklyProgressionPlanJson).IsRequired(false);

        builder.HasIndex(q => new { q.UserId, q.Type, q.QuestDateUtc }).IsUnique();
        builder.HasIndex(q => q.IdempotencyKey);

        // US-238: acelera GetLastCompletedByUserAndProgramAsync (busca do último concluído por programa).
        builder.HasIndex(q => new { q.UserId, q.ProgramId, q.CompletedAtUtc });

        builder.Ignore(q => q.IsConfirmed);

        builder.HasMany(q => q.Exercises)
            .WithOne()
            .HasForeignKey(e => e.QuestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(Quest.Exercises))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
