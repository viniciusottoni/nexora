using Awaken.Domain.Entities.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class ExerciseAttributeContributionConfiguration : IEntityTypeConfiguration<ExerciseAttributeContribution>
{
    public void Configure(EntityTypeBuilder<ExerciseAttributeContribution> builder)
    {
        builder.ToTable("exercise_attribute_contributions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PrimaryAttribute)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.ReviewedBy)
            .HasMaxLength(128);

        builder.HasIndex(e => e.ExerciseCatalogId)
            .IsUnique();
    }
}
