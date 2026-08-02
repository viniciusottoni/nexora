using Awaken.Domain.Entities.Nutrition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class UserNutritionPreferenceConfiguration : IEntityTypeConfiguration<UserNutritionPreference>
{
    public void Configure(EntityTypeBuilder<UserNutritionPreference> builder)
    {
        builder.ToTable("user_nutrition_preferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CupVolumeMl).IsRequired().HasDefaultValue(250);
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
