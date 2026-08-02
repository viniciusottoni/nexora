using Awaken.Domain.Entities.Exercises;

namespace Awaken.Domain.Services.Quests;

/// <summary>
/// US-045: filtro eliminatorio de seguranca. Remove exercicios incompativeis com
/// nivel, equipamento, tempo, limitacoes, dores, impacto, complexidade e aprovacao
/// antes de qualquer pontuacao (RN-001/RN-007 do EPIC-006). Roda sobre o catalogo
/// aprovado completo para que regressoes (RN-004) possam ser localizadas.
/// </summary>
public static class ExerciseSafetyFilter
{
    private static readonly Dictionary<string, int> ExperienceRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sedentary"] = 0,
        ["beginner"] = 1,
        ["intermediate"] = 2,
        ["advanced"] = 3,
    };

    private const decimal HighBmiThreshold = 25m;
    private const int HighImpactLevel = 4;
    private const int HighTechnicalComplexity = 4;

    public static IReadOnlyList<ExerciseCatalog> Apply(
        IReadOnlyList<ExerciseCatalog> candidates,
        ExerciseSafetyContext context)
    {
        // RN-003: apenas exercicios aprovados entram na pontuacao.
        var approved = candidates.Where(e => e.IsApprovedForWorkoutGeneration).ToList();
        var byProviderId = approved.ToDictionary(e => e.ProviderExerciseId);

        var eligible = new List<ExerciseCatalog>();
        var addedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var exercise in approved)
        {
            if (addedIds.Contains(exercise.ProviderExerciseId)) continue;

            if (IsEligible(exercise, context))
            {
                eligible.Add(exercise);
                addedIds.Add(exercise.ProviderExerciseId);
                continue;
            }

            // RN-004: preferir substituicao por regressao segura antes de remover.
            var regression = FindEligibleRegression(exercise, byProviderId, context, addedIds);
            if (regression is not null)
            {
                eligible.Add(regression);
                addedIds.Add(regression.ProviderExerciseId);
            }
            // 9.2: sem regressao disponivel, o exercicio e removido sem substituicao.
        }

        return eligible;
    }

    private static ExerciseCatalog? FindEligibleRegression(
        ExerciseCatalog exercise,
        IReadOnlyDictionary<string, ExerciseCatalog> byProviderId,
        ExerciseSafetyContext context,
        HashSet<string> addedIds)
    {
        foreach (var regressionId in exercise.RegressionExerciseIds)
        {
            if (addedIds.Contains(regressionId)) continue;
            if (!byProviderId.TryGetValue(regressionId, out var regression)) continue;
            if (IsEligible(regression, context)) return regression;
        }

        return null;
    }

    private static bool IsEligible(ExerciseCatalog exercise, ExerciseSafetyContext context)
    {
        if (!exercise.IsApprovedForWorkoutGeneration) return false;

        if (ExperienceRankOf(exercise.MinExperienceLevel) > ExperienceRankOf(context.EffectiveExperienceLevel))
            return false;

        if (!EquipmentSatisfied(exercise.RequiredEquipment, context.EquipmentAvailable))
            return false;

        if (EstimatedDurationMinutes(exercise) > context.AvailableMinutesPerWorkout)
            return false;

        if (HasConflict(exercise.LimitationBlockTags, context.PhysicalLimitations) ||
            HasConflict(exercise.ContraindicationTags, context.PhysicalLimitations))
            return false;

        if (HasConflict(exercise.PainBlockTags, context.PhysicalPains))
            return false;

        var isSedentary = string.Equals(context.EffectiveExperienceLevel, "sedentary", StringComparison.OrdinalIgnoreCase);
        var isBeginner = string.Equals(context.EffectiveExperienceLevel, "beginner", StringComparison.OrdinalIgnoreCase);

        if (isSedentary && context.Bmi is decimal bmi && bmi >= HighBmiThreshold && exercise.ImpactLevel >= HighImpactLevel)
            return false;

        if ((isSedentary || isBeginner) && exercise.TechnicalComplexity >= HighTechnicalComplexity)
            return false;

        return true;
    }

    private static int ExperienceRankOf(string level) =>
        ExperienceRank.TryGetValue(level, out var rank) ? rank : ExperienceRank["sedentary"];

    private static bool EquipmentSatisfied(
        IReadOnlyCollection<string> required,
        IReadOnlyCollection<string> available)
    {
        if (required.Count == 0) return true;

        var availableSet = available.Count == 0
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bodyweight" }
            : new HashSet<string>(available, StringComparer.OrdinalIgnoreCase);

        return required.All(r =>
            string.Equals(r, "bodyweight", StringComparison.OrdinalIgnoreCase) ||
            availableSet.Contains(r));
    }

    private static bool HasConflict(
        IReadOnlyCollection<string> exerciseTags,
        IReadOnlyCollection<string> profileTags) =>
        exerciseTags.Count > 0 && profileTags.Count > 0 &&
        exerciseTags.Any(t => profileTags.Contains(t, StringComparer.OrdinalIgnoreCase));

    private static int EstimatedDurationMinutes(ExerciseCatalog exercise)
    {
        var sets = exercise.DifficultyRank >= 3 ? 4 : 3;
        var reps = exercise.IsWeighted ? 10 : 12;
        var restSeconds = exercise.IsWeighted ? 90 : 60;
        var workSecondsPerSet = reps * 3;
        var totalSeconds = sets * (workSecondsPerSet + restSeconds);
        return (int)Math.Ceiling(totalSeconds / 60.0);
    }
}

/// <summary>
/// Recorte do perfil do usuario relevante para o filtro eliminatorio de
/// seguranca (US-045). O calculo do nivel efetivo (US-150) esta fora desta US;
/// <see cref="EffectiveExperienceLevel"/> recebe o nivel ja resolvido pelo chamador.
/// </summary>
public record ExerciseSafetyContext(
    string EffectiveExperienceLevel,
    IReadOnlyCollection<string> EquipmentAvailable,
    int AvailableMinutesPerWorkout,
    IReadOnlyCollection<string> PhysicalLimitations,
    IReadOnlyCollection<string> PhysicalPains,
    decimal? Bmi);
