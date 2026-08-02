using Awaken.Domain.Entities.Exercises;

namespace Awaken.Domain.Services.Quests;

/// <summary>
/// US-151: scores eligible exercises with safety-first weights.
/// US-152: integrates targetAttributeScore for low attributes and goal-linked attributes.
/// </summary>
public static class ExerciseScoringEngine
{
    private const string DefaultGoalTag = "maintenance";
    private const int LowAttributeThreshold = 2;

    private static readonly Dictionary<string, GoalProfile> GoalProfiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gain_muscle"] = new GoalProfile(
            ["strength", "hypertrophy"],
            ["strength", "vitality", "focus"]),
        ["build_muscle"] = new GoalProfile(
            ["strength", "hypertrophy"],
            ["strength", "vitality", "focus"]),
        ["gain_strength"] = new GoalProfile(
            ["strength"],
            ["strength", "focus", "vitality"]),
        ["more_strength"] = new GoalProfile(
            ["strength"],
            ["strength", "focus", "vitality"]),
        ["lose_weight"] = new GoalProfile(
            ["fat_loss", "conditioning", "maintenance"],
            ["endurance", "vitality", "strength"]),
        ["fat_loss"] = new GoalProfile(
            ["fat_loss", "conditioning", "maintenance"],
            ["endurance", "vitality", "strength"]),
        ["improve_conditioning"] = new GoalProfile(
            ["conditioning"],
            ["endurance", "vitality", "agility"]),
        ["conditioning"] = new GoalProfile(
            ["conditioning"],
            ["endurance", "vitality", "agility"]),
        ["stay_active"] = new GoalProfile(
            ["maintenance"],
            ["strength", "endurance", "agility", "vitality", "focus"]),
        ["maintain"] = new GoalProfile(
            ["maintenance"],
            ["strength", "endurance", "agility", "vitality", "focus"]),
        ["health_and_consistency"] = new GoalProfile(
            ["maintenance"],
            ["strength", "endurance", "agility", "vitality", "focus"]),
    };

    public static IReadOnlyList<ExerciseScore> Score(
        IReadOnlyList<ExerciseCatalog> eligibleExercises,
        ExerciseScoringContext context)
    {
        var effectiveRank = ExperienceRankOf(context.EffectiveExperienceLevel);
        var goalProfile = ResolveGoalProfile(context.Goal);
        var scores = new List<ExerciseScore>(eligibleExercises.Count);

        foreach (var exercise in eligibleExercises)
        {
            var goalAffinity = ComputeGoalAffinity(exercise, goalProfile);
            var safety = ComputeSafety(exercise);
            var levelMatch = ComputeLevelMatch(exercise, effectiveRank);
            var timeFit = ComputeTimeFit(exercise, context.AvailableMinutesPerWorkout);
            var variety = ComputeVariety(exercise);
            var progressionFit = ComputeProgressionFit(exercise, effectiveRank);
            var targetAttribute = ComputeTargetAttribute(exercise, goalProfile, context.UserAttributes);

            scores.Add(new ExerciseScore(
                exercise,
                goalAffinity,
                safety,
                levelMatch,
                timeFit,
                variety,
                progressionFit,
                targetAttribute));
        }

        return scores.OrderByDescending(s => s.StaticScore).ToList();
    }

    private static GoalProfile ResolveGoalProfile(string? goal)
    {
        if (goal is not null && GoalProfiles.TryGetValue(goal, out var profile))
            return profile;

        return new GoalProfile([DefaultGoalTag], ["strength", "endurance", "agility", "vitality", "focus"]);
    }

    private static double ComputeGoalAffinity(ExerciseCatalog exercise, GoalProfile goalProfile)
    {
        if (exercise.GoalTags.Count == 0) return 0.5;

        return exercise.GoalTags.Any(tag =>
                goalProfile.ExerciseGoalTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            ? 1.0
            : 0.0;
    }

    private static double ComputeSafety(ExerciseCatalog exercise)
    {
        // ImpactLevel: 1-5; TechnicalComplexity: 0-5. Higher values mean less safe.
        var normalizedImpact = (exercise.ImpactLevel - 1) / 4.0;
        var normalizedComplexity = exercise.TechnicalComplexity / 5.0;
        return 1.0 - (normalizedImpact * 0.6 + normalizedComplexity * 0.4);
    }

    private static double ComputeLevelMatch(ExerciseCatalog exercise, int effectiveRank)
    {
        var exerciseRank = ExperienceRankOf(exercise.MinExperienceLevel);
        var gap = Math.Abs(exerciseRank - effectiveRank);
        return Math.Max(0.0, 1.0 - gap / 3.0);
    }

    private static double ComputeTimeFit(ExerciseCatalog exercise, int availableMinutes)
    {
        if (availableMinutes <= 0) return 0.5;

        var estimated = EstimatedDurationMinutes(exercise);
        // Exercises that do not monopolize the budget get a higher score.
        return Math.Max(0.0, 1.0 - (double)estimated / availableMinutes);
    }

    private static double ComputeVariety(ExerciseCatalog exercise)
    {
        // Static proxy for variety. Greedy selection applies extra dynamic balance.
        var score = 0.5;
        if (exercise.IsCompound) score += 0.15;
        if (exercise.IsUnilateral) score += 0.15;
        if (exercise.SecondaryMuscleGroups.Count > 0) score += 0.20;
        return Math.Clamp(score, 0.0, 1.0);
    }

    private static double ComputeProgressionFit(ExerciseCatalog exercise, int effectiveRank)
    {
        // Penalizes exercises far above the user's level.
        var exerciseRank = ExperienceRankOf(exercise.MinExperienceLevel);
        var overshoot = Math.Max(0, exerciseRank - effectiveRank);
        return Math.Max(0.0, 1.0 - overshoot / 3.0);
    }

    private static double ComputeTargetAttribute(
        ExerciseCatalog exercise,
        GoalProfile goalProfile,
        IReadOnlyDictionary<string, int> userAttributes)
    {
        if (exercise.AttributeContribution is null) return 0.5;

        var contribution = exercise.AttributeContribution;
        var totalXp = GetScorableAttributeXp(contribution, "strength") +
                      GetScorableAttributeXp(contribution, "agility") +
                      GetScorableAttributeXp(contribution, "endurance") +
                      GetScorableAttributeXp(contribution, "vitality") +
                      GetScorableAttributeXp(contribution, "focus");

        if (totalXp <= 0) return 0.5;

        var targetXp = goalProfile.TargetAttributes.Sum(attr =>
            GetScorableAttributeXp(contribution, attr));

        var lowXp = userAttributes
            .Where(pair => pair.Value <= LowAttributeThreshold)
            .Sum(pair => GetScorableAttributeXp(contribution, pair.Key));

        var targetCoverage = (double)targetXp / totalXp;
        var lowCoverage = (double)lowXp / totalXp;

        return Math.Clamp((targetCoverage + lowCoverage) / 2.0, 0.0, 1.0);
    }

    private static int GetScorableAttributeXp(ExerciseAttributeContribution contribution, string attribute) =>
        attribute.ToLowerInvariant() switch
        {
            "strength" => contribution.StrengthXp,
            "agility" => contribution.AgilityXp,
            "endurance" => contribution.EnduranceXp,
            "vitality" => contribution.VitalityXp,
            "focus" => contribution.FocusXp,
            _ => 0
        };

    internal static int ExperienceRankOf(string level) =>
        ExperienceRank.TryGetValue(level, out var rank) ? rank : ExperienceRank["sedentary"];

    public static int EstimatedDurationMinutes(ExerciseCatalog exercise)
    {
        var sets = exercise.DifficultyRank >= 3 ? 4 : 3;
        var reps = exercise.IsWeighted ? 10 : 12;
        var restSeconds = exercise.IsWeighted ? 90 : 60;
        var totalSeconds = sets * (reps * 3 + restSeconds);
        return (int)Math.Ceiling(totalSeconds / 60.0);
    }

    private static readonly Dictionary<string, int> ExperienceRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sedentary"] = 0,
        ["beginner"] = 1,
        ["intermediate"] = 2,
        ["advanced"] = 3,
    };
}

/// <summary>
/// Computed score for an eligible exercise.
/// StaticScore follows the documented weights and selection adds dynamic variety.
/// </summary>
public sealed record ExerciseScore(
    ExerciseCatalog Exercise,
    double GoalAffinityScore,
    double SafetyScore,
    double LevelMatchScore,
    double TimeFitScore,
    double VarietyScore,
    double ProgressionFitScore,
    double TargetAttributeScore)
{
    // US-151 RN-001: safetyScore tem peso alto; targetAttribute entra com peso baixo na fórmula.
    public double StaticScore =>
        SafetyScore * 0.25 +
        GoalAffinityScore * 0.25 +
        LevelMatchScore * 0.15 +
        TargetAttributeScore * 0.15 +
        TimeFitScore * 0.10 +
        VarietyScore * 0.05 +
        ProgressionFitScore * 0.05;
}

/// <summary>
/// Context needed for exercise scoring.
/// UserAttributes: key = attribute name in lowercase (ex: "strength"), value = current level.
/// </summary>
public record ExerciseScoringContext(
    string? Goal,
    string EffectiveExperienceLevel,
    int AvailableMinutesPerWorkout,
    IReadOnlyDictionary<string, int> UserAttributes);

internal sealed record GoalProfile(
    IReadOnlyList<string> ExerciseGoalTags,
    IReadOnlyList<string> TargetAttributes);
