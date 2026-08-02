using Awaken.Domain.Entities.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class ExerciseRelationshipConfiguration : IEntityTypeConfiguration<ExerciseRelationship>
{
    public void Configure(EntityTypeBuilder<ExerciseRelationship> builder)
    {
        builder.ToTable("exercise_relationships");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RelatedProviderExerciseId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.RelatedName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.RelationKind)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Score)
            .HasPrecision(8, 2);

        builder.Property(e => e.Confidence)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.DatasetVersion)
            .HasMaxLength(32);

        builder.HasIndex(e => new { e.ExerciseCatalogId, e.RelationKind });
        builder.HasIndex(e => e.TargetExerciseCatalogId);
        builder.HasIndex(e => e.RelatedProviderExerciseId);

        builder.HasOne<ExerciseCatalog>()
            .WithMany()
            .HasForeignKey(e => e.TargetExerciseCatalogId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
