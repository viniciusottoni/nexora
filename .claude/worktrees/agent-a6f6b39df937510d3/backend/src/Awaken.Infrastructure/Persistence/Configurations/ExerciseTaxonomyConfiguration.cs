using Awaken.Domain.Entities.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class ExerciseTaxonomyConfiguration : IEntityTypeConfiguration<ExerciseTaxonomy>
{
    public void Configure(EntityTypeBuilder<ExerciseTaxonomy> builder)
    {
        builder.ToTable("exercise_taxonomies");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MovementFamily).HasMaxLength(128);
        builder.Property(e => e.MovementPattern).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Mechanic).HasMaxLength(64);
        builder.Property(e => e.ForceType).HasMaxLength(64);
        builder.Property(e => e.PlaneOfMotion).HasMaxLength(64);
        builder.Property(e => e.Laterality).HasMaxLength(64);
        builder.Property(e => e.BodyPosition).HasMaxLength(64);
        builder.Property(e => e.BenchAngle).HasMaxLength(64);
        builder.Property(e => e.EquipmentCategory).HasMaxLength(64);
        builder.Property(e => e.LoadType).HasMaxLength(64);
        builder.Property(e => e.PrimaryRegion).HasMaxLength(64);
        builder.Property(e => e.Confidence).IsRequired().HasMaxLength(32);

        builder.HasIndex(e => e.ExerciseCatalogId).IsUnique();
    }
}
