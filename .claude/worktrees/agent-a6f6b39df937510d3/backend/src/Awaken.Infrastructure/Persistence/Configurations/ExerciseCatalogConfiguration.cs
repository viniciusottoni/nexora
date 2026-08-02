using Awaken.Domain.Entities.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class ExerciseCatalogConfiguration : IEntityTypeConfiguration<ExerciseCatalog>
{
    public void Configure(EntityTypeBuilder<ExerciseCatalog> builder)
    {
        builder.ToTable("exercise_catalogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProviderName).IsRequired().HasMaxLength(64);
        builder.Property(e => e.ProviderExerciseId).IsRequired().HasMaxLength(128);
        builder.Property(e => e.ProviderVersion).HasMaxLength(32);
        builder.Property(e => e.NamePtBr).IsRequired().HasMaxLength(256);
        builder.Property(e => e.NameOriginal).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(256);
        builder.Property(e => e.DescriptionPtBr).HasColumnType("text");
        builder.Property(e => e.ExerciseType).IsRequired().HasMaxLength(64);
        builder.Property(e => e.DifficultyLevel).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Environment).IsRequired().HasMaxLength(64);
        builder.Property(e => e.VideoUrl).HasMaxLength(2048);
        builder.Property(e => e.ImageUrl).HasMaxLength(2048);
        builder.Property(e => e.GifUrl).HasMaxLength(2048);
        builder.Property(e => e.MediaLicenseInfo).HasMaxLength(512);
        builder.Property(e => e.SanitizationStatus).IsRequired().HasMaxLength(32);

        builder.HasIndex(e => new { e.ProviderName, e.ProviderExerciseId }).IsUnique();
        builder.HasIndex(e => e.Slug);
        builder.HasIndex(e => e.IsApprovedForWorkoutGeneration);

        builder.HasOne<ExerciseRawImport>()
            .WithMany()
            .HasForeignKey(e => e.RawImportId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.AttributeContribution)
            .WithOne()
            .HasForeignKey<ExerciseAttributeContribution>(e => e.ExerciseCatalogId)
            .OnDelete(DeleteBehavior.Cascade);

        // US-236: taxonomia extraída para tabela própria 1:1 (RN-003) — sem coluna própria aqui.
        builder.HasOne(e => e.Taxonomy)
            .WithOne()
            .HasForeignKey<ExerciseTaxonomy>(t => t.ExerciseCatalogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Relations)
            .WithOne()
            .HasForeignKey(e => e.ExerciseCatalogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
