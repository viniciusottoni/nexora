using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Nutrition;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Nutrition.Queries.GetBasicNutritionToday;

public class GetBasicNutritionTodayQueryHandler(
    ICurrentUserService currentUserService,
    IUserProfileRepository userProfileRepository,
    INutritionLogRepository nutritionLogRepository,
    IUserDateService userDateService,
    IUserNutritionPreferenceRepository nutritionPreferenceRepository) : IRequestHandler<GetBasicNutritionTodayQuery, BasicNutritionTodayResponse>
{
    private const int MinimumMlPerKg = 30;
    private const int IdealMlPerKg = 50;

    public async Task<BasicNutritionTodayResponse> Handle(
        GetBasicNutritionTodayQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken);

        var today = userDateService.TodayLocal;
        var log = await nutritionLogRepository.GetByUserIdAndDateAsync(userId, today, cancellationToken);
        var waterConsumedMl = log?.WaterMl ?? 0;

        // US-090 RN-004: preferência do volume do copo; padrão 250 ml.
        var preference = await nutritionPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        var cupVolumeMl = preference?.CupVolumeMl ?? 250;

        // US-086 RN-004: sem perfil ou sem peso válido, metas ficam zeradas e Flutter exibe perfil incompleto.
        // Consumo já registrado continua aparecendo para não perder dado.
        if (profile is null || profile.WeightKg is null or <= 0)
        {
            return new BasicNutritionTodayResponse(
                WaterMinimumMl: 0,
                WaterIdealMl: 0,
                WaterConsumedMl: waterConsumedMl,
                CupVolumeMl: cupVolumeMl);
        }

        // US-086 RN-001/RN-002: mínimo = 30 ml/kg, ideal = 50 ml/kg.
        var waterMinimumMl = (int)Math.Round(
            profile.WeightKg.Value * MinimumMlPerKg,
            MidpointRounding.AwayFromZero);
        var waterIdealMl = (int)Math.Round(
            profile.WeightKg.Value * IdealMlPerKg,
            MidpointRounding.AwayFromZero);

        // US-088: gasto calórico estimado.
        var (caloriesDay, caloriesNow, caloriesStatus) = CalculateCalories(profile, userDateService.NowLocal);

        return new BasicNutritionTodayResponse(
            WaterMinimumMl: waterMinimumMl,
            WaterIdealMl: waterIdealMl,
            WaterConsumedMl: waterConsumedMl,
            CaloriesSpentEstimatedToday: caloriesDay,
            CaloriesSpentEstimatedUntilNow: caloriesNow,
            CaloriesCalculationStatus: caloriesStatus,
            CupVolumeMl: cupVolumeMl);
    }

    /// <summary>
    /// US-088 RN-001 a RN-006: calcula TMB × fator de atividade × fator de tipo corporal × fração do dia.
    /// Retorna zeros e status "incomplete" se dados físicos insuficientes (RN-007).
    /// </summary>
    public static (int today, int untilNow, string status) CalculateCalories(
        Awaken.Domain.Entities.Onboarding.UserProfile profile,
        DateTime nowLocal)
    {
        var isMale = InterpretBiologicalSex(profile.BiologicalSex);
        if (isMale is null
            || profile.Age is null or <= 0
            || profile.HeightCm is null or <= 0
            || profile.WeightKg is null or <= 0)
        {
            return (0, 0, "incomplete");
        }

        // US-088 RN-001/RN-002: TMB por sexo biológico (Mifflin-St Jeor adaptado conforme spec).
        var weight = (double)profile.WeightKg.Value;
        var height = (double)profile.HeightCm.Value;
        var age = profile.Age.Value;

        var tmb = isMale.Value
            ? 10.0 * weight + 6.25 * height - 5.0 * age + 5.0
            : 10.0 * weight + 6.25 * height - 5.0 * age - 161.0;

        // US-088 RN-003: fator de atividade a partir de dias/semana disponíveis.
        var activityFactor = ActivityFactor(profile.AvailableDaysPerWeek);

        // US-088 RN-004: fator de tipo corporal.
        var bodyTypeFactor = BodyTypeFactor(profile.BodyType);

        // US-088 RN-005: gasto diário estimado.
        var dailyCalories = tmb * activityFactor * bodyTypeFactor;

        // US-088 RN-006: gasto até o momento = gasto diário × fração do dia transcorrida.
        var minutesElapsed = nowLocal.Hour * 60.0 + nowLocal.Minute + nowLocal.Second / 60.0;
        var fractionOfDay = minutesElapsed / 1440.0;
        var caloriesUntilNow = dailyCalories * fractionOfDay;

        return (
            (int)Math.Round(dailyCalories, MidpointRounding.AwayFromZero),
            (int)Math.Round(caloriesUntilNow, MidpointRounding.AwayFromZero),
            "estimated");
    }

    /// <summary>US-088 RN-007: interpreta sexo biológico. Null = não interpretável → perfil incompleto.</summary>
    public static bool? InterpretBiologicalSex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var s = value.Trim().ToLowerInvariant();
        return s switch
        {
            "masculino" or "male" or "m" or "homem" or "man" => true,
            "feminino" or "female" or "f" or "mulher" or "woman" => false,
            _ => null
        };
    }

    /// <summary>US-088 RN-003: mapeia dias/semana para fator de atividade.</summary>
    public static double ActivityFactor(int? daysPerWeek) => daysPerWeek switch
    {
        null or 0 => 1.20,
        1 or 2 => 1.35,
        3 or 4 => 1.55,
        5 or 6 => 1.75,
        _ => 1.90
    };

    /// <summary>US-088 RN-004: mapeia tipo corporal para fator multiplicador.</summary>
    public static double BodyTypeFactor(string? bodyType) => bodyType switch
    {
        "lean" => 0.95,
        "athletic_strong" => 1.07,
        "overweight" => 0.92,
        _ => 1.00
    };
}
