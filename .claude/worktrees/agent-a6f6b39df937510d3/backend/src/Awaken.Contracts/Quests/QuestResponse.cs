namespace Awaken.Contracts.Quests;

public record QuestResponse(
    Guid Id,
    string Type,
    string Status,
    string Language,
    DateTime QuestDateUtc,
    WorkoutDto? Workout,
    long XpAwarded,
    bool IsConfirmed,
    bool IsPersonalized,
    int RegenerationsUsed,
    int RegenerationLimit,
    int CompletedExerciseCount);

public record WorkoutDto(
    string Title,
    string Description,
    int DurationMinutes,
    IEnumerable<ExerciseDto> Exercises,
    // US-240: dia do programa resolvido pela rotação (US-238), para exibição do
    // rótulo do dia e aviso de recuperação (US-239) na tela de revisão do treino.
    // Nulos quando o programa não tem split clássico configurado (RN-009).
    string? ResolvedProgramKey = null,
    string? ResolvedDayKey = null,
    string? DayLabelI18nKey = null,
    string? SplitMapVersion = null,
    bool HasMuscleGroupInRecovery = false,
    // US-242: orçamento de tempo determinístico, para o aviso de "ajustado ao seu
    // tempo" / "micro quest" na revisão do treino (US-050).
    int? EstimatedDurationSeconds = null,
    string? TimeAdjustmentApplied = null,
    bool IsMicroQuest = false,
    // US-241: avisos de progressão semanal ("treino mais desafiador"/"semana de
    // deload") na revisão do treino (US-050).
    bool DeloadWeek = false,
    string? ProgressionDecision = null,
    bool RecalibratedFromProfileChange = false);

public record ExerciseDto(
    string Name,
    string Description,
    int Sets,
    int RepsMin,
    int? RepsMax,
    int? RestSeconds,
    string? VideoUrl,
    string? TargetRpe,
    // US-236: GIF 360 dedicado (preferido sobre VideoUrl na exibicao) e id do exercicio no
    // ExerciseCatalog, usado para consultar candidatos de relacao via
    // GET /api/exercises/{id}/relationships.
    string? GifUrl = null,
    string? ProviderExerciseId = null,
    // US-041: instrucoes passo-a-passo e dicas do exercicio, para a tela de pre-quest.
    IReadOnlyList<string>? Instructions = null,
    IReadOnlyList<string>? Tips = null);
