namespace Awaken.Domain.Services.Progression;

public record InitialHunterAttributes(
    int Strength,
    int Agility,
    int Endurance,
    int Vitality,
    int Focus,
    int Wisdom);

public record InitialHunterProgressionResult(
    InitialHunterAttributes Attributes,
    int RankScore,
    string Rank,
    int Level,
    bool RankCapApplied);

/// <summary>
/// US-156: deriva os atributos e o Rank/RankScore iniciais a partir das
/// respostas declaradas no onboarding, aplicando o teto de Rank B (EPIC-009).
/// </summary>
public static class OnboardingInitialProgressionCalculator
{
    private const int MinAttributePoints = 1;
    private const int MaxAttributePoints = 8;
    private const int AttributeCount = 6;

    private static readonly Dictionary<string, int> ExperienceLevelPoints = new()
    {
        ["sedentary"] = 1,
        ["beginner"] = 3,
        ["intermediate"] = 5,
        ["advanced"] = 8,
    };

    private static readonly Dictionary<string, int> TrainingDurationPoints = new()
    {
        ["does_not_train"] = 1,
        ["less_than_1_month"] = 2,
        ["1_6_months"] = 3,
        ["6_12_months"] = 5,
        ["more_than_1_year"] = 6,
        ["more_than_3_years"] = 8,
    };

    public static InitialHunterProgressionResult Calculate(string experienceLevel, string trainingDuration)
    {
        // RN-005: usa o nível efetivo conservador quando experiência e tempo de treino conflitam.
        var experiencePoints = ExperienceLevelPoints.GetValueOrDefault(experienceLevel, MinAttributePoints);
        var durationPoints = TrainingDurationPoints.GetValueOrDefault(trainingDuration, MinAttributePoints);
        var effectivePoints = Math.Clamp(
            Math.Min(experiencePoints, durationPoints),
            MinAttributePoints,
            MaxAttributePoints);

        var attributes = new InitialHunterAttributes(
            Strength: effectivePoints,
            Agility: effectivePoints,
            Endurance: effectivePoints,
            Vitality: effectivePoints,
            Focus: effectivePoints,
            Wisdom: effectivePoints);

        var rawRankScore = effectivePoints * AttributeCount;
        var rankCapApplied = rawRankScore >= RankCalculator.OnboardingRankScoreCap;
        var rankScore = Math.Min(rawRankScore, RankCalculator.OnboardingRankScoreCap);

        return new InitialHunterProgressionResult(
            Attributes: attributes,
            RankScore: rankScore,
            Rank: RankCalculator.CalculateRank(rankScore),
            Level: 1,
            RankCapApplied: rankCapApplied);
    }
}
