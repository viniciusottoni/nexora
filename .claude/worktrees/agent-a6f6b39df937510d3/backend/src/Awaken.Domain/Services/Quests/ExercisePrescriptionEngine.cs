namespace Awaken.Domain.Services.Quests;

/// <summary>
/// US-153: prescribes sets, reps and rest per user level and goal.
/// RN-007: sedentary/beginner get a fixed rep count (RepsMax = null).
/// RN-007: intermediate/advanced get a rep range [RepsMin, RepsMax].
/// </summary>
public static class ExercisePrescriptionEngine
{
    public static ExercisePrescription Prescribe(string effectiveExperienceLevel, string? goal)
    {
        var level = effectiveExperienceLevel.ToLowerInvariant();
        var normalizedGoal = (goal ?? string.Empty).ToLowerInvariant();

        return level switch
        {
            "beginner"     => PrescribeForBeginner(normalizedGoal),
            "intermediate" => PrescribeForIntermediate(normalizedGoal),
            "advanced"     => PrescribeForAdvanced(normalizedGoal),
            _              => PrescribeForSedentary(normalizedGoal), // sedentary + unknown → most conservative
        };
    }

    private static ExercisePrescription PrescribeForSedentary(string goal) => goal switch
    {
        "gain_muscle" or "build_muscle"          => new(Sets: 2, RepsMin: 10, RepsMax: null, RestSeconds: 75,  TargetRpe: "3-5"),
        "gain_strength" or "more_strength"       => new(Sets: 2, RepsMin: 8,  RepsMax: null, RestSeconds: 90,  TargetRpe: "3-5"),
        "lose_weight" or "fat_loss"              => new(Sets: 1, RepsMin: 12, RepsMax: null, RestSeconds: 45,  TargetRpe: "3-5"),
        "improve_conditioning" or "conditioning" => new(Sets: 1, RepsMin: 12, RepsMax: null, RestSeconds: 45,  TargetRpe: "3-5"),
        _                                        => new(Sets: 1, RepsMin: 10, RepsMax: null, RestSeconds: 60,  TargetRpe: "3-5"),
    };

    private static ExercisePrescription PrescribeForBeginner(string goal) => goal switch
    {
        "gain_muscle" or "build_muscle"          => new(Sets: 3, RepsMin: 12, RepsMax: null, RestSeconds: 60,  TargetRpe: "5-6"),
        "gain_strength" or "more_strength"       => new(Sets: 3, RepsMin: 10, RepsMax: null, RestSeconds: 75,  TargetRpe: "5-6"),
        "lose_weight" or "fat_loss"              => new(Sets: 2, RepsMin: 15, RepsMax: null, RestSeconds: 45,  TargetRpe: "5-6"),
        "improve_conditioning" or "conditioning" => new(Sets: 2, RepsMin: 15, RepsMax: null, RestSeconds: 45,  TargetRpe: "5-6"),
        _                                        => new(Sets: 2, RepsMin: 12, RepsMax: null, RestSeconds: 60,  TargetRpe: "5-6"),
    };

    private static ExercisePrescription PrescribeForIntermediate(string goal) => goal switch
    {
        "gain_muscle" or "build_muscle"          => new(Sets: 4, RepsMin: 10, RepsMax: 15, RestSeconds: 90,  TargetRpe: "6-8"),
        "gain_strength" or "more_strength"       => new(Sets: 4, RepsMin: 10, RepsMax: 12, RestSeconds: 150, TargetRpe: "7-8"),
        "lose_weight" or "fat_loss"              => new(Sets: 3, RepsMin: 15, RepsMax: 20, RestSeconds: 60,  TargetRpe: "6-8"),
        "improve_conditioning" or "conditioning" => new(Sets: 3, RepsMin: 15, RepsMax: 20, RestSeconds: 60,  TargetRpe: "6-8"),
        _                                        => new(Sets: 3, RepsMin: 10, RepsMax: 15, RestSeconds: 90,  TargetRpe: "6-8"),
    };

    private static ExercisePrescription PrescribeForAdvanced(string goal) => goal switch
    {
        "gain_muscle" or "build_muscle"          => new(Sets: 4, RepsMin: 8,  RepsMax: 12, RestSeconds: 120, TargetRpe: "7-9"),
        "gain_strength" or "more_strength"       => new(Sets: 5, RepsMin: 4,  RepsMax: 6,  RestSeconds: 180, TargetRpe: "8-9"),
        "lose_weight" or "fat_loss"              => new(Sets: 4, RepsMin: 15, RepsMax: 25, RestSeconds: 60,  TargetRpe: "7-8"),
        "improve_conditioning" or "conditioning" => new(Sets: 4, RepsMin: 20, RepsMax: 30, RestSeconds: 45,  TargetRpe: "7-8"),
        _                                        => new(Sets: 4, RepsMin: 10, RepsMax: 15, RestSeconds: 120, TargetRpe: "7-9"),
    };
}

public record ExercisePrescription(
    int Sets,
    int RepsMin,
    int? RepsMax,
    int RestSeconds,
    string TargetRpe);
