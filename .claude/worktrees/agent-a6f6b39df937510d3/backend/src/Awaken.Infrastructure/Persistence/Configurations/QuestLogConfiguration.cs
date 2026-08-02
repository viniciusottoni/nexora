using Awaken.Domain.Entities.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class QuestLogConfiguration : IEntityTypeConfiguration<QuestLog>
{
    public void Configure(EntityTypeBuilder<QuestLog> builder)
    {
        builder.ToTable("quest_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.QuestId).IsRequired();
        builder.Property(l => l.UserId).IsRequired();

        builder.Property(l => l.QuestType)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(l => l.XpEarned).IsRequired();

        builder.Property(l => l.StrengthXpEarned).IsRequired();
        builder.Property(l => l.AgilityXpEarned).IsRequired();
        builder.Property(l => l.EnduranceXpEarned).IsRequired();
        builder.Property(l => l.VitalityXpEarned).IsRequired();
        builder.Property(l => l.FocusXpEarned).IsRequired();
        builder.Property(l => l.WisdomXpEarned).IsRequired();

        builder.Property(l => l.StrengthPointsGranted).IsRequired();
        builder.Property(l => l.AgilityPointsGranted).IsRequired();
        builder.Property(l => l.EndurancePointsGranted).IsRequired();
        builder.Property(l => l.VitalityPointsGranted).IsRequired();
        builder.Property(l => l.FocusPointsGranted).IsRequired();

        builder.Property(l => l.ItemsEarnedJson).IsRequired(false);
        builder.Property(l => l.XpPenaltyApplied).IsRequired(false);
        builder.Property(l => l.CompletedAtUtc).IsRequired();

        // US-241: sentimento percebido pelo usuário na conclusão (too_easy/just_right/too_hard).
        builder.Property(l => l.PerceivedFeeling).IsRequired(false).HasMaxLength(16);

        builder.Ignore(l => l.ItemsEarned);

        builder.HasIndex(l => l.QuestId).IsUnique();
    }
}
