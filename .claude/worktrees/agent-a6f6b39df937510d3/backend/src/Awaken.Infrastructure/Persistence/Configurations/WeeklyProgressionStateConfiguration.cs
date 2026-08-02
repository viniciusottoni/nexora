using Awaken.Domain.Entities.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class WeeklyProgressionStateConfiguration : IEntityTypeConfiguration<WeeklyProgressionState>
{
    public void Configure(EntityTypeBuilder<WeeklyProgressionState> builder)
    {
        builder.ToTable("weekly_progression_states");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.WeekAnchorDate).IsRequired();
        builder.Property(x => x.MesocycleWeekIndex).IsRequired();
        builder.Property(x => x.ProfileSnapshotHash).HasMaxLength(64);
        builder.Property(x => x.ConsecutiveEasyWeeks).IsRequired();
        builder.Property(x => x.ConsecutiveHardWeeks).IsRequired();
        builder.Property(x => x.DeloadDue).IsRequired();
        builder.Property(x => x.LastDecision).HasMaxLength(16).IsRequired();
        builder.Property(x => x.LastAxis).HasMaxLength(32);
        builder.Property(x => x.VolumeSetsDelta).IsRequired();
        builder.Property(x => x.RpeDelta).IsRequired();
        builder.Property(x => x.RestSecondsDelta).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
