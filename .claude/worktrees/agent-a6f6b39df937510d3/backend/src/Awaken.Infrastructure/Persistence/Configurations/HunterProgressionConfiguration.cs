using Awaken.Domain.Entities.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class HunterProgressionConfiguration : IEntityTypeConfiguration<HunterProgression>
{
    public void Configure(EntityTypeBuilder<HunterProgression> builder)
    {
        builder.ToTable("hunter_progressions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.Rank).IsRequired().HasMaxLength(4);
        builder.Property(p => p.RankScore).IsRequired();
        builder.Property(p => p.StreakRankScoreBonus).IsRequired();

        // US-154: controle de limite mensal de ganho de RankScore.
        builder.Property(p => p.MonthlyRankScoreGain).IsRequired().HasDefaultValue(0);
        builder.Property(p => p.MonthlyRankScoreResetYearMonth).IsRequired().HasDefaultValue(0);
        builder.Property(p => p.HunterClass).IsRequired().HasMaxLength(64);
        builder.Property(p => p.Level).IsRequired();
        builder.Property(p => p.TotalXp).IsRequired();
        builder.Property(p => p.XpToNextLevel).IsRequired();
        builder.Property(p => p.CurrentStreakDays).IsRequired();
        builder.Property(p => p.LongestStreakDays).IsRequired();
        builder.Property(p => p.LastQuestCompletedAtUtc).IsRequired(false);
        builder.Property(p => p.ConsecutiveMissedDailyDays).IsRequired();
        builder.Property(p => p.RecentDailyPenaltyXp).IsRequired(false);
        builder.Property(p => p.RecentDailyPenaltyQuestDateUtc).IsRequired(false);

        builder.Property(p => p.Strength).IsRequired();
        builder.Property(p => p.Endurance).IsRequired();
        builder.Property(p => p.Agility).IsRequired();
        builder.Property(p => p.Vitality).IsRequired();
        builder.Property(p => p.Focus).IsRequired();
        builder.Property(p => p.Wisdom).IsRequired();

        // US-130: XP interno (0–9) por atributo.
        builder.Property(p => p.StrengthXp).IsRequired().HasDefaultValue(0);
        builder.Property(p => p.EnduranceXp).IsRequired().HasDefaultValue(0);
        builder.Property(p => p.AgilityXp).IsRequired().HasDefaultValue(0);
        builder.Property(p => p.VitalityXp).IsRequired().HasDefaultValue(0);
        builder.Property(p => p.FocusXp).IsRequired().HasDefaultValue(0);
        builder.Property(p => p.WisdomXp).IsRequired().HasDefaultValue(0);
    }
}
