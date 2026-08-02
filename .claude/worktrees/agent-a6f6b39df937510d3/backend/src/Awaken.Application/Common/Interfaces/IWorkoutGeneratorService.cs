using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;

namespace Awaken.Application.Common.Interfaces;

public interface IWorkoutGeneratorService
{
    /// US-241: <paramref name="userProfile"/>/<paramref name="hunterProgression"/> alimentam a
    /// reavaliação semanal (`WeeklyProgressionReviewer`) — nulos quando o chamador só tem o
    /// snapshot JSON (ex.: substituição de exercício), pulando a reavaliação nesse caso.
    Task<WorkoutGenerationResult> GenerateWorkoutJsonAsync(
        Guid userId,
        string language,
        string fitnessProfileJson,
        UserProfile? userProfile = null,
        HunterProgression? hunterProgression = null,
        CancellationToken cancellationToken = default);

    /// US-230: Pergaminho da Substituição — escolhe 1 exercício elegível
    /// (mesmo filtro de segurança/equipamento da geração original) que ainda
    /// não esteja na quest. Retorna null se não houver candidato elegível.
    Task<QuestExerciseSeed?> SelectSubstituteExerciseAsync(
        string fitnessProfileJson,
        IReadOnlyCollection<string> excludeProviderExerciseIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// US-044: <see cref="IsPersonalized"/> indica se o treino foi gerado a partir do
/// perfil (filtro de seguranca + catalogo) ou se caiu no fallback generico (US-046).
/// US-049: <see cref="GenerationMethod"/> e <see cref="AppliedFiltersJson"/> registram
/// como a quest foi montada, para auditoria de personalizacao/seguranca.
/// US-240: <see cref="ResolvedProgramKey"/>/<see cref="ResolvedDayKey"/>/
/// <see cref="ResolvedDayIndex"/>/<see cref="SplitMapVersion"/> refletem o dia do
/// programa resolvido pela rotação (US-238) para esta geração, e
/// <see cref="DailyWorkoutBlueprintJson"/> é o snapshot do blueprint (US-240) usado
/// para compor o conjunto elegível do dia. Nulos quando o programa não tem split
/// clássico configurado (RN-009) — aditivo, não quebra chamadores existentes.
/// </summary>
public record WorkoutGenerationResult(
    string WorkoutJson,
    bool IsPersonalized,
    string GenerationMethod,
    string AppliedFiltersJson,
    string? ResolvedProgramKey = null,
    string? ResolvedDayKey = null,
    int? ResolvedDayIndex = null,
    string? SplitMapVersion = null,
    string? DailyWorkoutBlueprintJson = null,
    // US-242: orçamento de tempo determinístico aplicado nesta geração (RN-007).
    int? EstimatedDurationSeconds = null,
    int? TimeBudgetSeconds = null,
    string? TimeAdjustmentApplied = null,
    string? WorkoutTimeModelVersion = null,
    // US-241: snapshot do WeeklyProgressionPlan usado nesta geração, para auditoria (RN da seção 14).
    string? WeeklyProgressionPlanJson = null);
