using Awaken.Domain.Entities.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class ExerciseRawImportConfiguration : IEntityTypeConfiguration<ExerciseRawImport>
{
    public void Configure(EntityTypeBuilder<ExerciseRawImport> builder)
    {
        builder.ToTable("exercise_raw_imports");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProviderName)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.ProviderExerciseId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.ProviderVersion)
            .HasMaxLength(32);

        builder.Property(e => e.RawJson)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(e => e.ImportedAtUtc)
            .IsRequired();

        builder.Property(e => e.ImportBatchId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.SourceUrl)
            .HasMaxLength(2048);

        builder.Property(e => e.SourceFilePath)
            .HasMaxLength(2048);

        builder.Property(e => e.MediaBaseUrl)
            .HasMaxLength(2048);

        builder.Property(e => e.MediaLicenseInfo)
            .HasMaxLength(512);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(16)
            .HasConversion<string>();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1024);

        builder.HasIndex(e => new { e.ProviderName, e.ProviderExerciseId })
            .IsUnique();

        builder.HasIndex(e => e.ImportBatchId);
    }
}
