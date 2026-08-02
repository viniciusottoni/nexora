using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Quests;

namespace Awaken.Application.Quests.Common;

internal static class QuestExerciseRewardMapper
{
    private static readonly (Func<QuestExercise, int> Getter, Action<QuestVisibleAttributeImpactsDtoBuilder, int> Setter)[]
        VisibleAttributeAccessors =
        [
            (e => e.StrengthXp, (b, value) => b.Strength = value),
            (e => e.AgilityXp, (b, value) => b.Agility = value),
            (e => e.EnduranceXp, (b, value) => b.Endurance = value),
            (e => e.VitalityXp, (b, value) => b.Vitality = value),
            (e => e.FocusXp, (b, value) => b.Focus = value),
        ];

    public static QuestExerciseRewardProjection Project(QuestExercise exercise)
    {
        var effectiveDifficulty = GetEffectiveDifficulty(exercise);
        var visibleBuilder = new QuestVisibleAttributeImpactsDtoBuilder();
        var visibleRawAttributes = VisibleAttributeAccessors
            .Select((accessor, index) => new
            {
                Index = index,
                Raw = accessor.Getter(exercise),
            })
            .Where(item => item.Raw > 0)
            .OrderByDescending(item => item.Raw)
            .ThenBy(item => item.Index)
            .Take(2)
            .ToList();

        if (visibleRawAttributes.Count > 0)
        {
            VisibleAttributeAccessors[visibleRawAttributes[0].Index]
                .Setter(visibleBuilder, effectiveDifficulty);
        }

        if (visibleRawAttributes.Count > 1)
        {
            VisibleAttributeAccessors[visibleRawAttributes[1].Index]
                .Setter(visibleBuilder, 1);
        }

        var visibleImpacts = visibleBuilder.Build();
        const int hiddenWisdom = 1;
        var xpEarned = new QuestAttributeXpDto(
            visibleImpacts.Strength,
            visibleImpacts.Agility,
            visibleImpacts.Endurance,
            visibleImpacts.Vitality,
            visibleImpacts.Focus,
            hiddenWisdom);

        return new QuestExerciseRewardProjection(
            EffectiveDifficulty: effectiveDifficulty,
            AttributeXpEarned: xpEarned,
            AttributePointsGranted: visibleImpacts,
            VisibleImpacts: visibleImpacts,
            HiddenImpacts: new QuestHiddenAttributeImpactsDto(hiddenWisdom));
    }

    public static QuestAttributeXpDto Summarize(IReadOnlyList<QuestExercise> exercises) =>
        exercises
            .Select(Project)
            .Aggregate(
                new QuestAttributeXpDto(0, 0, 0, 0, 0, 0),
                (acc, projection) => new QuestAttributeXpDto(
                    acc.Strength + projection.AttributeXpEarned.Strength,
                    acc.Agility + projection.AttributeXpEarned.Agility,
                    acc.Endurance + projection.AttributeXpEarned.Endurance,
                    acc.Vitality + projection.AttributeXpEarned.Vitality,
                    acc.Focus + projection.AttributeXpEarned.Focus,
                    acc.Wisdom + projection.AttributeXpEarned.Wisdom));

    private static int GetEffectiveDifficulty(QuestExercise exercise) =>
        Math.Clamp((int)Math.Ceiling(exercise.XpReward / 4.0), 1, 4);
}

internal sealed record QuestExerciseRewardProjection(
    int EffectiveDifficulty,
    QuestAttributeXpDto AttributeXpEarned,
    QuestVisibleAttributeImpactsDto AttributePointsGranted,
    QuestVisibleAttributeImpactsDto VisibleImpacts,
    QuestHiddenAttributeImpactsDto HiddenImpacts);

internal sealed class QuestVisibleAttributeImpactsDtoBuilder
{
    public int Strength { get; set; }
    public int Agility { get; set; }
    public int Endurance { get; set; }
    public int Vitality { get; set; }
    public int Focus { get; set; }

    public QuestVisibleAttributeImpactsDto Build() =>
        new(Strength, Agility, Endurance, Vitality, Focus);
}
