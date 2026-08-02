namespace Awaken.Domain.Services.Progression;

/// <summary>
/// US-150: deriva o effectiveExperienceLevel a partir de experienceLevel e
/// trainingDuration, sempre escolhendo o nível mais conservador (RN-004) em
/// caso de conflito entre o que o usuário declara e o tempo de treino real.
/// </summary>
public static class ExperienceLevelCalculator
{
    private const string Sedentary = "sedentary";
    private const string Beginner = "beginner";
    private const string Intermediate = "intermediate";
    private const string Advanced = "advanced";

    private static readonly Dictionary<string, int> LevelRank = new()
    {
        [Sedentary] = 0,
        [Beginner] = 1,
        [Intermediate] = 2,
        [Advanced] = 3,
    };

    private static readonly string[] LevelsByRank = [Sedentary, Beginner, Intermediate, Advanced];

    // RN-001/RN-002/RN-003: tempo de treino implica um nível máximo plausível,
    // independente do que o usuário declarou em experienceLevel.
    private static readonly Dictionary<string, string> TrainingDurationImpliedLevel = new()
    {
        ["does_not_train"] = Sedentary,
        ["less_than_1_month"] = Beginner,
        ["1_6_months"] = Beginner,
        ["6_12_months"] = Intermediate,
        ["more_than_1_year"] = Intermediate,
        ["more_than_3_years"] = Advanced,
    };

    public static string CalculateEffectiveLevel(string? experienceLevel, string? trainingDuration)
    {
        var declaredRank = RankOf(experienceLevel);
        var impliedLevel = TrainingDurationImpliedLevel.GetValueOrDefault(trainingDuration ?? string.Empty, Sedentary);
        var impliedRank = RankOf(impliedLevel);

        // RN-004: em conflito, vence o nível mais seguro (menor rank).
        var effectiveRank = Math.Min(declaredRank, impliedRank);
        return LevelsByRank[effectiveRank];
    }

    private static int RankOf(string? level) =>
        LevelRank.GetValueOrDefault(level ?? string.Empty, LevelRank[Sedentary]);
}
