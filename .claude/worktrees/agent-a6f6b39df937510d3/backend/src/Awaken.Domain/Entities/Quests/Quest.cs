using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Quests;

public class Quest : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = "daily";
    public string Status { get; private set; } = "pending";
    public string Language { get; private set; } = "pt-BR";
    public string? IdempotencyKey { get; private set; }
    public DateTime QuestDateUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public long XpAwarded { get; private set; }

    // US-059: marca pausa/retomada da execucao em andamento.
    public DateTime? PausedAtUtc { get; private set; }
    public DateTime? ResumedAtUtc { get; private set; }

    // US-060: marca cancelamento da execucao em andamento ou pausada.
    public DateTime? CancelledAtUtc { get; private set; }
    public string? WorkoutJson { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public bool IsPersonalized { get; private set; } = true;

    // US-048: contador de regeneracoes do dia (RN-001/RN-003).
    public int RegenerationCount { get; private set; }

    // US-049: motivo/metodo e snapshot de auditoria da ultima geracao/regeneracao.
    public string? GenerationReason { get; private set; }
    public string? GenerationMethod { get; private set; }
    public string? ProfileSnapshotJson { get; private set; }
    public string? AppliedFiltersJson { get; private set; }

    // US-129: marca que a virada de dia ja processou (penalizou ou nao) esta daily, evitando reprocessamento.
    public DateTime? PenaltyCheckedAtUtc { get; private set; }

    // US-051: tipo de treino escolhido antes de iniciar (personalized_individual, regeneration, program).
    public string TrainingType { get; private set; } = "personalized_individual";

    // US-051: identificador do programa, preenchido apenas quando TrainingType == "program".
    public string? ProgramId { get; private set; }

    // US-238: dia do programa resolvido pela rotação cíclica sobre o histórico
    // de treinos concluídos (RN-009), para auditoria e base da próxima rotação.
    public string? ResolvedProgramKey { get; private set; }
    public string? ResolvedDayKey { get; private set; }
    public int? ResolvedDayIndex { get; private set; }
    public string? SplitMapVersion { get; private set; }

    // US-240: snapshot do DailyWorkoutBlueprint (alvo ponderado, orçamento de
    // volume, restrições) usado para gerar esta quest, para auditoria (RN-008/US-049).
    public string? DailyWorkoutBlueprintJson { get; private set; }

    // US-242: orçamento de tempo determinístico usado para compor esta quest (RN-007).
    public int? EstimatedDurationSeconds { get; private set; }
    public int? TimeBudgetSeconds { get; private set; }
    public string? TimeAdjustmentApplied { get; private set; }
    public string? WorkoutTimeModelVersion { get; private set; }

    // US-241: snapshot do WeeklyProgressionPlan usado para gerar esta quest (auditoria).
    public string? WeeklyProgressionPlanJson { get; private set; }

    public bool IsConfirmed => ConfirmedAtUtc.HasValue;

    private readonly List<QuestExercise> _exercises = [];
    public IReadOnlyList<QuestExercise> Exercises => _exercises.AsReadOnly();

    private Quest() { }

    public static Quest Create(Guid userId, DateTime questDateUtc, string language, string idempotencyKey)
    {
        return new Quest
        {
            UserId = userId,
            QuestDateUtc = questDateUtc,
            Language = language,
            IdempotencyKey = idempotencyKey
        };
    }

    public void AssignWorkout(string workoutJson, DateTime utcNow, bool isPersonalized = true)
    {
        WorkoutJson = workoutJson;
        IsPersonalized = isPersonalized;
        UpdatedAtUtc = utcNow;
    }

    /// US-049: registra motivo/metodo e snapshot de auditoria da geracao atual.
    public void RegisterGenerationAudit(
        string generationReason,
        string generationMethod,
        string profileSnapshotJson,
        string appliedFiltersJson,
        DateTime utcNow)
    {
        GenerationReason = generationReason;
        GenerationMethod = generationMethod;
        ProfileSnapshotJson = profileSnapshotJson;
        AppliedFiltersJson = appliedFiltersJson;
        UpdatedAtUtc = utcNow;
    }

    /// US-048: substitui o treino do dia por uma nova geracao (regeneracao),
    /// mantendo perfil/seguranca/tempo (RN-002) e contabilizando a tentativa (RN-001).
    /// <paramref name="viaReforgeScroll"/> indica regeneracao alem do limite diario,
    /// concedida pelo consumo do item "Pergaminho da Reforja" (RN-003 + obs do item).
    public void Regenerate(string workoutJson, bool isPersonalized, bool viaReforgeScroll, DateTime utcNow)
    {
        WorkoutJson = workoutJson;
        IsPersonalized = isPersonalized;
        if (!viaReforgeScroll)
        {
            RegenerationCount++;
        }

        UpdatedAtUtc = utcNow;
    }

    public void Confirm(DateTime utcNow)
    {
        if (IsConfirmed)
            return;
        ConfirmedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// US-056: idempotente - iniciar uma quest ja em andamento nao reescreve StartedAtUtc.
    /// US-057: materializa QuestExercise ordenados a partir do WorkoutJson na 1a chamada.
    public void Start(DateTime utcNow, IEnumerable<QuestExerciseSeed> exerciseSeeds)
    {
        if (Status == "in_progress")
            return;

        Status = "in_progress";
        StartedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        if (_exercises.Count == 0)
        {
            var order = 1;
            foreach (var seed in exerciseSeeds)
            {
                _exercises.Add(QuestExercise.Create(Id, order, seed));
                order++;
            }
        }
    }

    public void Complete(long xpAwarded, DateTime utcNow)
    {
        Status = "completed";
        CompletedAtUtc = utcNow;
        XpAwarded = xpAwarded;
        UpdatedAtUtc = utcNow;
    }

    /// US-059 RN-001/RN-003: pausa uma quest em andamento, sem conceder XP.
    /// Idempotente - pausar uma quest ja pausada nao reescreve PausedAtUtc.
    public void Pause(DateTime utcNow)
    {
        if (Status == "paused")
            return;

        Status = "paused";
        PausedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// US-059 RN-002/RN-004: retoma uma quest pausada, sem conceder XP.
    /// Idempotente - retomar uma quest ja em andamento nao reescreve ResumedAtUtc.
    public void Resume(DateTime utcNow)
    {
        if (Status == "in_progress")
            return;

        Status = "in_progress";
        ResumedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// US-060 RN-002/RN-004: cancela uma quest em andamento ou pausada, impedindo
    /// conclusao posterior e recompensa completa. Idempotente.
    public void Cancel(DateTime utcNow)
    {
        if (Status == "cancelled")
            return;

        Status = "cancelled";
        CancelledAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// US-051: troca o tipo de treino antes de iniciar a quest, gerando/substituindo o treino inteiro.
    /// Só pode ser chamado enquanto status == "pending" (quest não iniciada).
    public void ChangeTrainingType(
        string trainingType,
        string? programId,
        string workoutJson,
        bool isPersonalized,
        DateTime utcNow)
    {
        TrainingType = trainingType;
        ProgramId = programId;
        WorkoutJson = workoutJson;
        IsPersonalized = isPersonalized;
        UpdatedAtUtc = utcNow;
    }

    public void MarkPenaltyChecked(DateTime utcNow)
    {
        PenaltyCheckedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// US-238: registra o dia do programa resolvido pela rotação para esta geração.
    public void AssignResolvedProgramDay(string programKey, string dayKey, int dayIndex, string splitMapVersion, DateTime utcNow)
    {
        TrainingType = "program";
        ProgramId = programKey;
        ResolvedProgramKey = programKey;
        ResolvedDayKey = dayKey;
        ResolvedDayIndex = dayIndex;
        SplitMapVersion = splitMapVersion;
        UpdatedAtUtc = utcNow;
    }

    /// US-240: registra o snapshot do DailyWorkoutBlueprint usado para compor
    /// esta geração (RN-008), para auditoria (US-049).
    public void AssignDailyWorkoutBlueprint(string dailyWorkoutBlueprintJson, DateTime utcNow)
    {
        DailyWorkoutBlueprintJson = dailyWorkoutBlueprintJson;
        UpdatedAtUtc = utcNow;
    }

    /// US-242: registra o orçamento de tempo aplicado a esta geração, para auditoria (RN-007).
    public void AssignTimeBudget(
        int estimatedDurationSeconds, int timeBudgetSeconds, string timeAdjustmentApplied,
        string workoutTimeModelVersion, DateTime utcNow)
    {
        EstimatedDurationSeconds = estimatedDurationSeconds;
        TimeBudgetSeconds = timeBudgetSeconds;
        TimeAdjustmentApplied = timeAdjustmentApplied;
        WorkoutTimeModelVersion = workoutTimeModelVersion;
        UpdatedAtUtc = utcNow;
    }

    /// US-241: registra o snapshot do WeeklyProgressionPlan usado para compor esta geração.
    public void AssignWeeklyProgressionPlan(string weeklyProgressionPlanJson, DateTime utcNow)
    {
        WeeklyProgressionPlanJson = weeklyProgressionPlanJson;
        UpdatedAtUtc = utcNow;
    }
}
