using Awaken.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class TrainingProgramConfiguration : IEntityTypeConfiguration<TrainingProgram>
{
    public void Configure(EntityTypeBuilder<TrainingProgram> builder)
    {
        builder.ToTable("training_programs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.Category).HasMaxLength(256);
        builder.Property(x => x.MinimumRank).HasMaxLength(8).IsRequired();
        builder.Property(x => x.SplitDays);
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
