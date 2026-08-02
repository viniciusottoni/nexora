using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Nutrition;

public class UserNutritionPreference : BaseEntity
{
    public Guid UserId { get; private set; }
    public int CupVolumeMl { get; private set; }

    private UserNutritionPreference() { }

    public static UserNutritionPreference Create(Guid userId, int cupVolumeMl = 250)
    {
        return new UserNutritionPreference
        {
            UserId = userId,
            CupVolumeMl = cupVolumeMl,
        };
    }

    /// <summary>US-090 RN-001/RN-004: atualiza volume do copo. Validação na camada Application.</summary>
    public void UpdateCupVolume(int cupVolumeMl) => CupVolumeMl = cupVolumeMl;
}
