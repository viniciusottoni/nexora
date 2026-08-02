using Awaken.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class TrainingProgramSplitConfiguration : IEntityTypeConfiguration<TrainingProgramSplit>
{
    public void Configure(EntityTypeBuilder<TrainingProgramSplit> builder)
    {
        builder.ToTable("training_program_splits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProgramKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SplitMapVersion).HasMaxLength(16).IsRequired();
        builder.Property(x => x.DayCount).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.ProgramKey).IsUnique();

        builder.HasMany(s => s.Days)
            .WithOne()
            .HasForeignKey(d => d.TrainingProgramSplitId)
            .OnDelete(DeleteBehavior.Cascade);

        // `Days` é uma propriedade calculada (_days.AsReadOnly()), sem setter —
        // o EF precisa ler/escrever direto no campo privado que a sustenta.
        builder.Metadata.FindNavigation(nameof(TrainingProgramSplit.Days))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
