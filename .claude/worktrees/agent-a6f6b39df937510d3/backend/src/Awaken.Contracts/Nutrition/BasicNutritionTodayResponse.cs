namespace Awaken.Contracts.Nutrition;

/// <summary>US-086/US-088/US-090: meta diária de água, consumo, gasto calórico estimado e volume do copo.</summary>
public record BasicNutritionTodayResponse(
    int WaterMinimumMl,
    int WaterIdealMl,
    int WaterConsumedMl,
    int CaloriesSpentEstimatedToday = 0,
    int CaloriesSpentEstimatedUntilNow = 0,
    string CaloriesCalculationStatus = "incomplete",
    int CupVolumeMl = 250);
