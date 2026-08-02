using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Nutrition;

public class NutritionLog : BaseEntity
{
    public Guid UserId { get; private set; }
    public DateOnly Date { get; private set; }
    public int WaterMl { get; private set; }

    private NutritionLog() { }

    public static NutritionLog Create(Guid userId, DateOnly date)
    {
        return new NutritionLog
        {
            UserId = userId,
            Date = date,
            WaterMl = 0,
        };
    }

    /// <summary>US-087 RN-002: soma volume em ml. Ignora valores nao positivos.</summary>
    public void AddWater(int amountMl)
    {
        if (amountMl <= 0) return;
        WaterMl += amountMl;
    }
}
