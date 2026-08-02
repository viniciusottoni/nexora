using Awaken.Domain.Entities.Nutrition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class NutritionLogConfiguration : IEntityTypeConfiguration<NutritionLog>
{
    public void Configure(EntityTypeBuilder<NutritionLog> builder)
    {
        builder.ToTable("nutrition_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.WaterMl).IsRequired().HasDefaultValue(0);

        // US-087 RN-001: unicidade por usuário + data.
        builder.HasIndex(x => new { x.UserId, x.Date }).IsUnique();
    }
}
