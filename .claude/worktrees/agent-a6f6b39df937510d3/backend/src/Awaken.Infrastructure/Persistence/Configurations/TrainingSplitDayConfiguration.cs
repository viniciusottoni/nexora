using Awaken.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class TrainingSplitDayConfiguration : IEntityTypeConfiguration<TrainingSplitDay>
{
    public void Configure(EntityTypeBuilder<TrainingSplitDay> builder)
    {
        builder.ToTable("training_split_days");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DayIndex).IsRequired();
        builder.Property(x => x.DayKey).HasMaxLength(8).IsRequired();
        builder.Property(x => x.LabelI18nKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Role).HasMaxLength(32).IsRequired();
        builder.Property(x => x.AllowsCoreFinisher).IsRequired();
        builder.Property(x => x.MinExercises).IsRequired();
        builder.Property(x => x.MaxExercises).IsRequired();

        builder.HasIndex(x => new { x.TrainingProgramSplitId, x.DayIndex }).IsUnique();
    }
}
