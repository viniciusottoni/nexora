using Awaken.Domain.Entities.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class RankScoreLogConfiguration : IEntityTypeConfiguration<RankScoreLog>
{
    public void Configure(EntityTypeBuilder<RankScoreLog> builder)
    {
        builder.ToTable("rank_score_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.UserId).IsRequired();
        builder.HasIndex(l => l.UserId);

        builder.Property(l => l.Source).IsRequired().HasMaxLength(32);
        builder.Property(l => l.RawGain).IsRequired();
        builder.Property(l => l.Multiplier).IsRequired().HasColumnType("numeric(5,2)");
        builder.Property(l => l.ExternalMultiplier).IsRequired().HasColumnType("numeric(5,2)");
        builder.Property(l => l.EffectiveGain).IsRequired();
        builder.Property(l => l.WasMonthlyLimitApplied).IsRequired();
        builder.Property(l => l.WasAbuseSuspected).IsRequired();
        builder.Property(l => l.RankScoreAfter).IsRequired();
    }
}
