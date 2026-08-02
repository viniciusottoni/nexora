using Awaken.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class MuscleRecoveryStateConfiguration : IEntityTypeConfiguration<MuscleRecoveryState>
{
    public void Configure(EntityTypeBuilder<MuscleRecoveryState> builder)
    {
        builder.ToTable("muscle_recovery_states");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MuscleGroup).HasMaxLength(32).IsRequired();
        builder.Property(x => x.LastTrainedAtUtc).IsRequired();
        builder.Property(x => x.LastIntensity).HasMaxLength(16).IsRequired();
        builder.Property(x => x.WeeklySetsAccumulated).IsRequired();
        builder.Property(x => x.WeekAnchorDateUtc).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.MuscleGroup }).IsUnique();
    }
}
