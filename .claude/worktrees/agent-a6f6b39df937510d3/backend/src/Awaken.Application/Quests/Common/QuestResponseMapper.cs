using System.Linq;
using System.Text.Json;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Services.Quests;

namespace Awaken.Application.Quests.Common;

public static class QuestResponseMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static QuestPreviewResponse ToPreviewResponse(Quest quest)
    {
        var workout = ParseWorkout(quest.WorkoutJson);
        var estimatedDurationMinutes = workout?.DurationMinutes ?? 0;
        var estimatedXp = (long)Math.Round(estimatedDurationMinutes * 4.0);
        // US-051: TrainingType explÃ­cito quando alterado; fallback para retrocompatibilidade.
        var trainingType = quest.TrainingType != "personalized_individual"
            ? quest.TrainingType
            : quest.IsPersonalized ? "personalized_individual" : "fallback";
        var canChangeTrainingType = quest.Status == "pending";

        return new QuestPreviewResponse(
            QuestId: quest.Id,
            QuestType: quest.Type,
            TrainingType: trainingType,
            EstimatedXp: estimatedXp,
            EstimatedDurationMinutes: estimatedDurationMinutes,
            CanChangeTrainingType: canChangeTrainingType,
            Workout: workout);
    }

    public static QuestResponse ToResponse(Quest quest)
    {
        return new QuestResponse(
            Id: quest.Id,
            Type: quest.Type,
            Status: quest.Status,
            Language: quest.Language,
            QuestDateUtc: quest.QuestDateUtc,
            Workout: ParseWorkout(quest.WorkoutJson),
            XpAwarded: quest.XpAwarded,
            IsConfirmed: quest.IsConfirmed,
            IsPersonalized: quest.IsPersonalized,
            RegenerationsUsed: quest.RegenerationCount,
            RegenerationLimit: QuestRegenerationPolicy.DailyFreeLimit,
            CompletedExerciseCount: quest.Exercises.Count(e => e.Status == "completed"));
    }

    private static WorkoutDto? ParseWorkout(string? workoutJson)
    {
        if (string.IsNullOrWhiteSpace(workoutJson)) return null;

        var raw = JsonSerializer.Deserialize<RawWorkout>(workoutJson, JsonOptions);
        if (raw is null) return null;

        return new WorkoutDto(
            Title: raw.Title ?? string.Empty,
            Description: raw.Description ?? string.Empty,
            DurationMinutes: raw.DurationMinutes ?? 0,
            Exercises: (raw.Exercises ?? []).Select(e => new ExerciseDto(
                Name: e.Name ?? string.Empty,
                Description: e.Description ?? string.Empty,
                Sets: e.Sets ?? 0,
                RepsMin: e.RepsMin ?? e.Reps ?? 0,   // backward compat: old JSON had 'reps' only
                RepsMax: e.RepsMax,
                RestSeconds: e.RestSeconds,
                VideoUrl: e.VideoUrl ?? e.ImageUrl,
                TargetRpe: e.TargetRpe,
                // US-236: GIF 360 tem campo proprio (nao cai mais no fallback de VideoUrl) + id do
                // exercicio no catalogo, para a tela de exercicio consultar candidatos de relacao.
                GifUrl: e.GifUrl,
                ProviderExerciseId: e.Id,
                // US-041: instrucoes passo-a-passo e dicas geradas por WorkoutGeneratorService
                // dentro do proprio WorkoutJson - antes desta correcao, se perdiam aqui.
                Instructions: e.Instructions ?? [],
                Tips: e.Tips ?? [])),
            // US-240: dia do programa resolvido pela rotação, emitido por
            // WorkoutGeneratorService dentro do próprio JSON do treino.
            ResolvedProgramKey: raw.ResolvedProgramKey,
            ResolvedDayKey: raw.ResolvedDayKey,
            DayLabelI18nKey: raw.DayLabelI18nKey,
            SplitMapVersion: raw.SplitMapVersion,
            HasMuscleGroupInRecovery: raw.HasMuscleGroupInRecovery ?? false,
            EstimatedDurationSeconds: raw.EstimatedDurationSeconds,
            TimeAdjustmentApplied: raw.TimeAdjustmentApplied,
            IsMicroQuest: raw.IsMicroQuest ?? false,
            DeloadWeek: raw.DeloadWeek ?? false,
            ProgressionDecision: raw.ProgressionDecision,
            RecalibratedFromProfileChange: raw.RecalibratedFromProfileChange ?? false);
    }

    private class RawWorkout
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? DurationMinutes { get; set; }
        public List<RawExercise>? Exercises { get; set; }
        public string? ResolvedProgramKey { get; set; }
        public string? ResolvedDayKey { get; set; }
        public string? DayLabelI18nKey { get; set; }
        public string? SplitMapVersion { get; set; }
        public bool? HasMuscleGroupInRecovery { get; set; }
        public int? EstimatedDurationSeconds { get; set; }
        public string? TimeAdjustmentApplied { get; set; }
        public bool? IsMicroQuest { get; set; }
        public bool? DeloadWeek { get; set; }
        public string? ProgressionDecision { get; set; }
        public bool? RecalibratedFromProfileChange { get; set; }
    }

    private class RawExercise
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Sets { get; set; }
        public int? Reps { get; set; }       // backward compat for quests stored before US-153
        public int? RepsMin { get; set; }
        public int? RepsMax { get; set; }
        public int? RestSeconds { get; set; }
        public string? VideoUrl { get; set; }
        public string? GifUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? TargetRpe { get; set; }
        // US-041: instrucoes passo-a-passo e dicas do exercicio, gravadas por
        // WorkoutGeneratorService dentro do proprio WorkoutJson.
        public List<string>? Instructions { get; set; }
        public List<string>? Tips { get; set; }
    }
}


