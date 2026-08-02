namespace Awaken.Domain.Services.Quests;

/// <summary>
/// US-040: ponte de vocabulario entre as tags de onboarding (<see cref="CompleteOnboardingCommandValidator"/>
/// - PhysicalLimitations/PhysicalPains, ex.: "knee_problem", "back") e as tags de risco/articulacao do
/// catalogo de exercicios (ex.: "knee_high_stress", "lumbar_high_stress" - ver
/// ImportExercisesCommandHandler.BuildJointStressTags/BuildRiskTags). Sem esta traducao,
/// ExerciseSafetyFilter.HasConflict compara string exata entre vocabularios que nunca coincidem
/// com dado real, e o filtro de seguranca por dor/limitacao nunca bloqueia nada na pratica.
/// </summary>
public static class OnboardingTagTranslator
{
    // Baseado no vocabulario real de CompleteOnboardingCommandValidator.cs
    private static readonly IReadOnlyDictionary<string, string[]> LimitationMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["disk_herniation"] = ["lumbar_high_stress"],
        ["knee_problem"] = ["knee_high_stress"],
        ["no_impact"] = ["high_impact"],
        ["shoulder_injury"] = ["shoulder_high_stress"],
        ["chronic_lumbar_pain"] = ["lumbar_high_stress"],
        // conservador de proposito: restricao medica generica bloqueia as categorias de risco mais comuns.
        ["medical_restriction"] = ["lumbar_high_stress", "shoulder_high_stress", "knee_high_stress", "high_impact", "high_technical_complexity"],
    };

    private static readonly IReadOnlyDictionary<string, string[]> PainMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["neck"] = ["cervical_high_stress"],
        ["shoulder"] = ["shoulder_high_stress"],
        ["wrist"] = ["wrist_high_stress"],
        ["back"] = ["lumbar_high_stress"],
        ["lower_back"] = ["lumbar_high_stress"],
        ["knees"] = ["knee_high_stress"],
    };

    public static IReadOnlyCollection<string> TranslateLimitations(IEnumerable<string> onboardingValues) =>
        Translate(onboardingValues, LimitationMap);

    public static IReadOnlyCollection<string> TranslatePains(IEnumerable<string> onboardingValues) =>
        Translate(onboardingValues, PainMap);

    private static IReadOnlyCollection<string> Translate(IEnumerable<string> values, IReadOnlyDictionary<string, string[]> map)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            // valor desconhecido passa adiante sem traducao (nao apaga informacao, so nao expande).
            if (map.TryGetValue(value, out var catalogTags))
            {
                foreach (var tag in catalogTags) result.Add(tag);
            }
            else
            {
                result.Add(value);
            }
        }
        return result;
    }
}
