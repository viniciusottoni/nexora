using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Training;
using Awaken.Domain.Repositories;
using Awaken.Domain.Services.Training;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Awaken.Application.Quests.Common;

/// US-240: alvo ponderado do dia (grupos + padrões + orçamento de volume +
/// restrições), emitido para o pipeline de geração (US-045 -> US-151 -> US-153).
/// Não é a lista final de exercícios: a segurança (US-045) ainda pode remover
/// candidatos e o tempo (US-151) ainda limita a quantidade (RN-005).
public record DailyWorkoutBlueprintTargetGroup(
    string MuscleGroup,
    decimal Weight,
    int VolumeBudgetSets,
    int RpeMax,
    // US-239: grupo abaixo do status "Recovered" no momento da composição -
    // sinaliza a UI (US-240 5.3) e é reservado para a pontuação (US-151/152).
    bool IsInRecovery,
    IReadOnlyList<string> AvoidMovementFamilies);

public record DailyWorkoutBlueprint(
    string ProgramKey,
    string ResolvedDayKey,
    int ResolvedDayIndex,
    bool RestDay,
    bool FallbackUsed,
    string SplitMapVersion,
    IReadOnlyList<DailyWorkoutBlueprintTargetGroup> TargetGroups,
    IReadOnlyList<string> TargetPatterns,
    int EligibleExerciseCount,
    string DayLabelI18nKey = "",
    string? FullBodyEmphasis = null);

/// US-240: orquestrador que combina split (US-237) + rotação do dia (US-238) +
/// plano de recuperação (US-239) + catálogo aprovado (US-035) num
/// `DailyWorkoutBlueprint` determinístico (RN-001), consumido por
/// `WorkoutGeneratorService` antes do filtro de segurança (US-045).
/// Fica em Application (não é serviço de domínio puro) pois orquestra
/// repositórios via interface — mesmo precedente de `FitnessProfileSnapshot`/
/// `QuestRegenerationService` neste mesmo namespace.
public class DailyWorkoutBlueprintBuilder(
    IUserWorkoutPreferenceRepository preferenceRepository,
    ITrainingProgramSplitRepository splitRepository,
    IQuestRepository questRepository,
    IMuscleRecoveryStateRepository recoveryRepository,
    IExerciseCatalogRepository catalogRepository,
    IDateTimeService dateTimeService,
    ILogger<DailyWorkoutBlueprintBuilder> logger)
{
    private const int DefaultRpeMax = 9;

    public async Task<DailyWorkoutBlueprint> BuildAsync(
        Guid userId, string effectiveExperienceLevel, CancellationToken ct = default)
    {
        var preference = await preferenceRepository.GetByUserIdAsync(userId, ct);
        // RN-006: sem programa selecionado, default por nível/rank -> full_body.
        var programKey = preference?.PreferredProgramId ?? TrainingProgramKeys.FullBody;

        var split = await splitRepository.GetByProgramKeyAsync(programKey, ct);
        if (split is null)
        {
            // RN-009 (US-238)/programa sem split classico (ex.: perfect_2, system):
            // sem blueprint estruturado -> fallback trivial, catalogo completo segue como antes.
            // R4 (analytics): fallback por ausencia de split configurado para o programa.
            logger.LogInformation(
                "daily_blueprint_fallback userId={UserId} programKey={ProgramKey} reason=no_split_configured",
                userId, programKey);

            return new DailyWorkoutBlueprint(
                programKey, "N/A", ResolvedDayIndex: 0, RestDay: false, FallbackUsed: true,
                SplitMapVersion: "n/a", TargetGroups: [], TargetPatterns: [], EligibleExerciseCount: 0);
        }

        var days = split.Days.OrderBy(d => d.DayIndex).Select(d => d.DayKey).ToList();
        var lastCompleted = await questRepository.GetLastCompletedByUserAndProgramAsync(userId, programKey, ct);
        var resolution = DailyProgramDayResolver.Resolve(days, lastCompleted?.ResolvedDayKey);
        var dayTarget = split.Days.First(d => d.DayKey == resolution.DayKey);
        var fullBodyEmphasis = programKey == TrainingProgramKeys.FullBody
            ? NextFullBodyEmphasis(lastCompleted?.DailyWorkoutBlueprintJson)
            : null;

        var recoveryStates = await recoveryRepository.GetByUserIdAsync(userId, ct);
        var recoveryByGroup = recoveryStates.ToDictionary(s => s.MuscleGroup, StringComparer.Ordinal);
        var utcNow = dateTimeService.UtcNow;
        var perSessionCap = RecoveryPlanner.PerSessionSetCapFor(effectiveExperienceLevel);

        // RN-002/US-237 §6.2: grupos primários + secundários do dia, sem duplicar,
        // primários com peso pleno e secundários com peso reduzido.
        var allGroups = dayTarget.TargetMuscleGroups
            .Concat(dayTarget.SecondaryMuscleGroups)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // R4 (analytics): grupos cujo capFactor efetivo ficou abaixo de 1.0 (teto reduzido por
        // recuperacao/volume semanal, US-239) - coletados para o log agregado overload_guard_applied.
        var overloadGuardGroups = new List<string>();

        var targetGroups = allGroups.Select(group =>
        {
            var state = recoveryByGroup.GetValueOrDefault(group);
            var status = RecoveryPlanner.StatusFor(state, utcNow);
            var (capFactor, rpeDelta) = RecoveryPlanner.ModulationFor(status);
            var isWeeklyVolumeCapped = state is not null
                && state.WeeklySetsAccumulated >= RecoveryPlanner.WeeklySetCapFor(effectiveExperienceLevel);
            if (isWeeklyVolumeCapped)
            {
                capFactor = Math.Min(capFactor, 0.5m);
                rpeDelta = Math.Min(rpeDelta, -1);
            }

            if (capFactor < 1.0m)
                overloadGuardGroups.Add(group);

            IReadOnlyList<string> avoidFamilies = state?.LastMovementFamilies ?? [];

            return new DailyWorkoutBlueprintTargetGroup(
                MuscleGroup: group,
                Weight: WeightFor(group, dayTarget, fullBodyEmphasis),
                VolumeBudgetSets: Math.Max(1, (int)Math.Round(perSessionCap * capFactor)),
                RpeMax: Math.Clamp(DefaultRpeMax + rpeDelta, 5, DefaultRpeMax),
                IsInRecovery: status != MuscleRecoveryStatus.Recovered || isWeeklyVolumeCapped,
                AvoidMovementFamilies: avoidFamilies);
        }).ToList();

        // R4 (analytics): plano de recuperacao (US-239) aplicado ao alvo do dia - 1 log agregado,
        // nao por grupo muscular.
        logger.LogInformation(
            "recovery_plan_generated userId={UserId} programKey={ProgramKey} groupCount={GroupCount} groupsInRecovery={GroupsInRecovery}",
            userId, programKey, targetGroups.Count, targetGroups.Count(g => g.IsInRecovery));

        if (overloadGuardGroups.Count > 0)
        {
            logger.LogInformation(
                "overload_guard_applied userId={UserId} programKey={ProgramKey} muscleGroups={MuscleGroups}",
                userId, programKey, string.Join(",", overloadGuardGroups));
        }

        // RN-002: candidatos coerentes com o dia (grupo E padrão do split). A
        // preferência por evitar `avoidMovementFamilies` (RN-004) é sinalizada
        // por grupo acima e fica para a etapa de pontuação (US-151/152) decidir
        // como preterir sem eliminar - esta contagem mede só a coerência dura
        // de grupo/padrão que define suficiência (RN-007) e fallback (US-046).
        var approvedCatalog = await catalogRepository.ListApprovedForWorkoutGenerationAsync(ct);
        var eligibleCount = approvedCatalog.Count(e =>
            e.PrimaryMuscleGroups.Any(g => dayTarget.TargetMuscleGroups.Contains(g)) &&
            dayTarget.TargetMovementPatterns.Contains(e.MovementPattern));

        // RN-007/US-046: sem elegíveis suficientes para o mínimo do dia, aciona
        // fallback em vez de compor um treino incoerente.
        var fallbackUsed = eligibleCount < dayTarget.MinExercises;
        // RestDay ainda nao e produzido por nenhum fluxo real (ver R4) - variavel (nao const) de
        // proposito, para nao gerar CS0162 no branch abaixo, que fica pronto para quando existir.
        bool restDay = false;

        // R4 (analytics): resultado final da composicao do blueprint do dia.
        if (restDay)
        {
            logger.LogInformation(
                "daily_blueprint_rest_day userId={UserId} programKey={ProgramKey} resolvedDayKey={ResolvedDayKey}",
                userId, programKey, resolution.DayKey);
        }
        else if (fallbackUsed)
        {
            logger.LogInformation(
                "daily_blueprint_fallback userId={UserId} programKey={ProgramKey} resolvedDayKey={ResolvedDayKey} reason=insufficient_eligible_exercises eligibleCount={EligibleCount} minExercises={MinExercises}",
                userId, programKey, resolution.DayKey, eligibleCount, dayTarget.MinExercises);
        }
        else
        {
            logger.LogInformation(
                "daily_blueprint_composed userId={UserId} programKey={ProgramKey} resolvedDayKey={ResolvedDayKey} eligibleCount={EligibleCount}",
                userId, programKey, resolution.DayKey, eligibleCount);
        }

        return new DailyWorkoutBlueprint(
            ProgramKey: programKey,
            ResolvedDayKey: resolution.DayKey,
            ResolvedDayIndex: resolution.DayIndex,
            RestDay: restDay,
            FallbackUsed: fallbackUsed,
            SplitMapVersion: split.SplitMapVersion,
            TargetGroups: targetGroups,
            TargetPatterns: dayTarget.TargetMovementPatterns,
            EligibleExerciseCount: eligibleCount,
            DayLabelI18nKey: dayTarget.LabelI18nKey,
            FullBodyEmphasis: fullBodyEmphasis);
    }

    private static decimal WeightFor(string group, TrainingSplitDay dayTarget, string? fullBodyEmphasis)
    {
        var baseWeight = dayTarget.SecondaryMuscleGroups.Contains(group) ? 0.6m : 1.0m;
        if (fullBodyEmphasis is null)
            return baseWeight;

        string[] emphasizedGroups = fullBodyEmphasis switch
        {
            "push" => [MuscleGroups.Chest, MuscleGroups.Shoulders, MuscleGroups.Triceps],
            "pull" => [MuscleGroups.Back, MuscleGroups.Biceps, MuscleGroups.RearDelts, MuscleGroups.Traps],
            "legs" => [MuscleGroups.Quadriceps, MuscleGroups.Hamstrings, MuscleGroups.Glutes, MuscleGroups.Calves],
            _ => []
        };

        return emphasizedGroups.Contains(group) ? baseWeight + 0.25m : baseWeight;
    }

    private static string NextFullBodyEmphasis(string? lastBlueprintJson)
    {
        var previous = TryReadFullBodyEmphasis(lastBlueprintJson);
        return previous switch
        {
            "push" => "pull",
            "pull" => "legs",
            "legs" => "push",
            _ => "push"
        };
    }

    private static string? TryReadFullBodyEmphasis(string? blueprintJson)
    {
        if (string.IsNullOrWhiteSpace(blueprintJson))
            return null;

        try
        {
            using var document = JsonDocument.Parse(blueprintJson);
            return document.RootElement.TryGetProperty("fullBodyEmphasis", out var emphasis)
                && emphasis.ValueKind == JsonValueKind.String
                ? emphasis.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
