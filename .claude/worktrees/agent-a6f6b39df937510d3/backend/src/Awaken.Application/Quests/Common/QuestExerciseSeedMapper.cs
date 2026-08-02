using System.Text.Json;
using Awaken.Domain.Entities.Quests;

namespace Awaken.Application.Quests.Common;

/// US-057/US-058: parser separado do QuestResponseMapper, pois precisa capturar campos
/// (id, attributeContribution) que o mapper de leitura ignora propositalmente.
public static class QuestExerciseSeedMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static List<QuestExerciseSeed> ParseSeeds(string? workoutJson)
    {
        if (string.IsNullOrWhiteSpace(workoutJson)) return [];

        try
        {
            var raw = JsonSerializer.Deserialize<RawWorkoutForSeed>(workoutJson, JsonOptions);
            if (raw?.Exercises is null) return [];

            return raw.Exercises.Select(ToSeed).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static QuestExerciseSeed ToSeed(RawExerciseForSeed e)
    {
        var sets = e.Sets ?? 0;
        var repsMin = e.RepsMin ?? e.Reps ?? 0; // backward compat: old JSON had 'reps' only

        var attr = e.AttributeContribution;
        int strengthXp, agilityXp, enduranceXp, vitalityXp, focusXp, wisdomXp;
        long baseXp;

        if (attr is not null)
        {
            strengthXp = attr.StrengthXp ?? 0;
            agilityXp = attr.AgilityXp ?? 0;
            enduranceXp = attr.EnduranceXp ?? 0;
            vitalityXp = attr.VitalityXp ?? 0;
            focusXp = attr.FocusXp ?? 0;
            wisdomXp = attr.WisdomXp ?? 0;
            baseXp = strengthXp + agilityXp + enduranceXp + vitalityXp + focusXp + wisdomXp;
        }
        else
        {
            // Fallback (sem catálogo): RN-005/US-058 garante Sabedoria sempre concedida.
            strengthXp = 0;
            agilityXp = 0;
            enduranceXp = 0;
            vitalityXp = 0;
            focusXp = 0;
            wisdomXp = 1;
            baseXp = 1;
        }

        var xpReward = Math.Max(1, (long)Math.Round(baseXp * sets * repsMin / 10.0));

        return new QuestExerciseSeed(
            Name: e.Name ?? string.Empty,
            ExerciseCatalogProviderId: e.Id,
            Sets: sets,
            RepsMin: repsMin,
            RepsMax: e.RepsMax,
            RestSeconds: e.RestSeconds,
            TargetRpe: e.TargetRpe,
            VideoUrl: e.GifUrl ?? e.VideoUrl ?? e.ImageUrl,
            XpReward: xpReward,
            StrengthXp: strengthXp,
            AgilityXp: agilityXp,
            EnduranceXp: enduranceXp,
            VitalityXp: vitalityXp,
            FocusXp: focusXp,
            WisdomXp: wisdomXp);
    }

    private class RawWorkoutForSeed
    {
        public List<RawExerciseForSeed>? Exercises { get; set; }
    }

    private class RawExerciseForSeed
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int? Sets { get; set; }
        public int? Reps { get; set; } // backward compat for quests stored before US-153
        public int? RepsMin { get; set; }
        public int? RepsMax { get; set; }
        public int? RestSeconds { get; set; }
        public string? VideoUrl { get; set; }
        public string? GifUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? TargetRpe { get; set; }
        public RawAttributeContributionForSeed? AttributeContribution { get; set; }
    }

    private class RawAttributeContributionForSeed
    {
        public string? PrimaryAttribute { get; set; }
        public int? StrengthXp { get; set; }
        public int? AgilityXp { get; set; }
        public int? EnduranceXp { get; set; }
        public int? VitalityXp { get; set; }
        public int? FocusXp { get; set; }
        public int? WisdomXp { get; set; }
    }
}
