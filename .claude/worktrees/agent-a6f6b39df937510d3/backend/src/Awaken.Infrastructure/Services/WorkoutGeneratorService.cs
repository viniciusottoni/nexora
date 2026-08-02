using Awaken.Application.Common.Interfaces;
using Awaken.Application.Progression.Common;
using Awaken.Application.Quests.Common;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using Awaken.Domain.Services.Progression;
using Awaken.Domain.Services.Quests;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Awaken.Infrastructure.Services;

public class WorkoutGeneratorService(
    ILogger<WorkoutGeneratorService> logger,
    IExerciseCatalogRepository exerciseCatalogRepository,
    DailyWorkoutBlueprintBuilder dailyWorkoutBlueprintBuilder,
    WeeklyProgressionReviewer weeklyProgressionReviewer) : IWorkoutGeneratorService
{
    private static readonly string[] NoLimitationSentinels = ["no_limitations"];
    private static readonly string[] NoPainSentinels = ["no_pains"];
    private static readonly JsonSerializerOptions CamelCaseJsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<WorkoutGenerationResult> GenerateWorkoutJsonAsync(
        Guid userId,
        string language,
        string fitnessProfileJson,
        UserProfile? userProfile = null,
        HunterProgression? hunterProgression = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: integrate OpenAI. Selection below relies on the approved catalog.
        logger.LogInformation("Generating workout for user {UserId} in language {Language}", userId, language);

        var profile = ParseProfile(fitnessProfileJson);
        var exerciseCount = ResolveExerciseCount(profile.AvailableMinutesPerWorkout);

        // US-241: reavaliação semanal (progressão/deload/recalibração por perfil) -
        // só roda quando o chamador tem o perfil/progressão reais disponíveis
        // (GenerateDailyQuestCommandHandler); nulo para chamadores que só têm o
        // snapshot JSON (ex.: SelectSubstituteExerciseAsync não usa este método).
        var weeklyPlan = userProfile is not null
            ? await weeklyProgressionReviewer.ReviewAsync(userId, userProfile, hunterProgression, cancellationToken)
            : null;

        var approvedExercises = await exerciseCatalogRepository.ListApprovedForWorkoutGenerationAsync(cancellationToken);

        // US-240: alvo do dia (programa + rotação + recuperação), calculado antes do
        // filtro de segurança (RN-005: segurança permanece soberana e roda depois).
        var blueprint = await dailyWorkoutBlueprintBuilder.BuildAsync(
            userId, profile.EffectiveExperienceLevel, cancellationToken);
        var candidateExercises = ApplyBlueprintCoherenceFilter(approvedExercises, blueprint);

        // US-045: filtro eliminatorio de seguranca roda antes de qualquer selecao.
        var context = new ExerciseSafetyContext(
            EffectiveExperienceLevel: profile.EffectiveExperienceLevel,
            EquipmentAvailable: profile.EquipmentAvailable,
            AvailableMinutesPerWorkout: profile.AvailableMinutesPerWorkout,
            PhysicalLimitations: profile.PhysicalLimitations,
            PhysicalPains: profile.PhysicalPains,
            Bmi: profile.Bmi);

        var eligibleExercises = ExerciseSafetyFilter.Apply(candidateExercises, context);

        // US-049: snapshot dos filtros de seguranca aplicados, para auditoria
        // de respeito a limitacoes/dores (RN-002/CA-002), sem dados sensiveis (ADR-014).
        var appliedFiltersJson = JsonSerializer.Serialize(new
        {
            effectiveExperienceLevel = context.EffectiveExperienceLevel,
            equipmentAvailable = context.EquipmentAvailable,
            availableMinutesPerWorkout = context.AvailableMinutesPerWorkout,
            physicalLimitations = context.PhysicalLimitations,
            physicalPains = context.PhysicalPains,
            candidateCount = approvedExercises.Count,
            eligibleCount = eligibleExercises.Count,
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // US-240 RN-008: o dia resolvido pela rotação segue sendo registrado mesmo
        // quando a geração cai no fallback genérico - a rotação (US-238) não pode
        // travar no mesmo dia só porque a personalização falhou desta vez.
        var (resolvedProgramKey, resolvedDayKey, resolvedDayIndex, splitMapVersion, blueprintJson) =
            BuildBlueprintAuditFields(blueprint);

        // US-242: fallback usa um orçamento de tempo conservador (sem ladder de ajuste -
        // a lista de exercícios do fallback já é pequena e fixa via ResolveExerciseCount).
        var fallbackIsMicroQuest = profile.AvailableMinutesPerWorkout <= WorkoutTimeModel.MicroQuestThresholdMinutes;
        var fallbackTimeBudgetSeconds = profile.AvailableMinutesPerWorkout * 60;

        if (blueprint.FallbackUsed && blueprint.ResolvedDayKey != "N/A")
        {
            logger.LogWarning(
                "Daily workout blueprint had insufficient coherent exercises for user {UserId}; using fallback template.", userId);
            return new WorkoutGenerationResult(
                BuildFallbackWorkoutJson(
                    language,
                    profile.EffectiveExperienceLevel,
                    profile.Goal,
                    profile.AvailableMinutesPerWorkout,
                    exerciseCount,
                    resolvedProgramKey,
                    resolvedDayKey,
                    blueprint.DayLabelI18nKey,
                    splitMapVersion,
                    blueprint.TargetGroups.Any(g => g.IsInRecovery),
                    weeklyPlan),
                IsPersonalized: false,
                GenerationMethod: "fallback_template",
                AppliedFiltersJson: appliedFiltersJson,
                ResolvedProgramKey: resolvedProgramKey,
                ResolvedDayKey: resolvedDayKey,
                ResolvedDayIndex: resolvedDayIndex,
                SplitMapVersion: splitMapVersion,
                DailyWorkoutBlueprintJson: blueprintJson,
                EstimatedDurationSeconds: fallbackTimeBudgetSeconds,
                TimeBudgetSeconds: fallbackTimeBudgetSeconds,
                TimeAdjustmentApplied: fallbackIsMicroQuest ? "micro_quest" : "none",
                WorkoutTimeModelVersion: WorkoutTimeModel.Version,
                WeeklyProgressionPlanJson: weeklyPlan is null ? null : JsonSerializer.Serialize(weeklyPlan, CamelCaseJsonOptions));
        }

        if (eligibleExercises.Count == 0)
        {
            // 9.1 (US-045) / US-046: lista inviavel apos o filtro aciona o fallback.
            logger.LogWarning(
                "No eligible exercises after the safety filter for user {UserId}; using fallback template.", userId);
            return new WorkoutGenerationResult(
                BuildFallbackWorkoutJson(
                    language,
                    profile.EffectiveExperienceLevel,
                    profile.Goal,
                    profile.AvailableMinutesPerWorkout,
                    exerciseCount,
                    resolvedProgramKey,
                    resolvedDayKey,
                    blueprint.DayLabelI18nKey,
                    splitMapVersion,
                    blueprint.TargetGroups.Any(g => g.IsInRecovery),
                    weeklyPlan),
                IsPersonalized: false,
                GenerationMethod: "fallback_template",
                AppliedFiltersJson: appliedFiltersJson,
                ResolvedProgramKey: resolvedProgramKey,
                ResolvedDayKey: resolvedDayKey,
                ResolvedDayIndex: resolvedDayIndex,
                SplitMapVersion: splitMapVersion,
                DailyWorkoutBlueprintJson: blueprintJson,
                EstimatedDurationSeconds: fallbackTimeBudgetSeconds,
                TimeBudgetSeconds: fallbackTimeBudgetSeconds,
                TimeAdjustmentApplied: fallbackIsMicroQuest ? "micro_quest" : "none",
                WorkoutTimeModelVersion: WorkoutTimeModel.Version,
                WeeklyProgressionPlanJson: weeklyPlan is null ? null : JsonSerializer.Serialize(weeklyPlan, CamelCaseJsonOptions));
        }

        // US-151: pontua e seleciona com peso alto de segurança, respeitando orçamento de tempo.
        // US-152: targetAttributeScore direciona atributos baixos e ligados ao objetivo.
        var scoringContext = new ExerciseScoringContext(
            Goal: profile.Goal,
            EffectiveExperienceLevel: profile.EffectiveExperienceLevel,
            AvailableMinutesPerWorkout: profile.AvailableMinutesPerWorkout,
            UserAttributes: profile.UserAttributes);

        var scored = ExerciseScoringEngine.Score(eligibleExercises, scoringContext);
        var selectedScores = ExerciseSelectionEngine.Select(scored, profile.AvailableMinutesPerWorkout);
        var selectedExercises = selectedScores.Select(s => s.Exercise).ToList();

        // US-153: prescrição de séries/reps/descanso por nível efetivo e objetivo.
        var basePrescription = ExercisePrescriptionEngine.Prescribe(profile.EffectiveExperienceLevel, profile.Goal);

        // US-241: aplica no máximo 1 eixo de ajuste (RN-003) sobre a prescrição base.
        // O teto de séries do blueprint (US-239/US-240, `volumeBudgetByGroup` abaixo)
        // continua soberano — aplicado depois, em `SetsFor` (Math.Min já existente).
        var prescription = ApplyWeeklyProgressionPlan(basePrescription, weeklyPlan);

        // US-240: orçamento de volume por grupo (RN-003) - cada exercício respeita o
        // teto do seu grupo muscular primário quando o cap do blueprint é mais baixo
        // que a prescrição padrão (ex.: grupo em recuperação, US-239).
        var volumeBudgetByGroup = blueprint.TargetGroups
            .ToDictionary(g => g.MuscleGroup, g => g.VolumeBudgetSets, StringComparer.Ordinal);
        var rpeMaxByGroup = blueprint.TargetGroups
            .ToDictionary(g => g.MuscleGroup, g => g.RpeMax, StringComparer.Ordinal);
        var hasMuscleGroupInRecovery = blueprint.TargetGroups.Any(g => g.IsInRecovery);

        int SetsFor(ExerciseCatalog exercise)
        {
            var primaryGroup = exercise.PrimaryMuscleGroups.FirstOrDefault();
            return primaryGroup is not null
                && volumeBudgetByGroup.TryGetValue(primaryGroup, out var budgetSets)
                && budgetSets < prescription.Sets
                ? budgetSets
                : prescription.Sets;
        }

        string? TargetRpeFor(ExerciseCatalog exercise)
        {
            var primaryGroup = exercise.PrimaryMuscleGroups.FirstOrDefault();
            if (primaryGroup is null
                || !rpeMaxByGroup.TryGetValue(primaryGroup, out var rpeMax)
                || string.IsNullOrWhiteSpace(prescription.TargetRpe)
                || !int.TryParse(prescription.TargetRpe, out var prescribedRpe))
            {
                return prescription.TargetRpe;
            }

            return Math.Min(prescribedRpe, rpeMax).ToString();
        }

        // US-242: orçamento de tempo determinístico - roda depois da seleção/prescrição
        // reais (as heurísticas de ExerciseSafetyFilter/ExerciseScoringEngine continuam
        // como pré-filtro aproximado interno; TimeBudgetCalculator é a fonte de verdade
        // exposta/persistida). Pode remover/adicionar exercícios e ajustar séries/descanso.
        // PriorityScore reflete a ORDEM da seleção gulosa (US-151, ExerciseSelectionEngine.Select) -
        // não o StaticScore isolado - para que o ladder de tempo corte primeiro o que a seleção já
        // considerou menos prioritário (fim da lista), preservando a escolha real da seleção
        // (que pondera variedade além do score estático).
        var selectedItems = selectedScores.Select((s, index) => new TimeBudgetItem(
            ExerciseId: s.Exercise.ProviderExerciseId,
            Sets: SetsFor(s.Exercise),
            Reps: prescription.RepsMax ?? prescription.RepsMin,
            PlannedDurationSeconds: null,
            RestSeconds: prescription.RestSeconds,
            PriorityScore: selectedScores.Count - index)).ToList();

        var extraCandidateItems = scored
            .Where(s => !selectedScores.Contains(s))
            .Select(s => new TimeBudgetItem(
                s.Exercise.ProviderExerciseId, prescription.Sets, prescription.RepsMax ?? prescription.RepsMin,
                null, prescription.RestSeconds, s.StaticScore))
            .ToList();

        var timeBudgetResolution = TimeBudgetCalculator.Resolve(new TimeBudgetRequest(
            SelectedItems: selectedItems,
            ExtraCandidates: extraCandidateItems,
            EffectiveExperienceLevel: profile.EffectiveExperienceLevel,
            Goal: profile.Goal,
            AvailableMinutesPerWorkout: profile.AvailableMinutesPerWorkout));

        var timeBudgetByExerciseId = timeBudgetResolution.Items.ToDictionary(i => i.ExerciseId);
        var byProviderId = scored.ToDictionary(s => s.Exercise.ProviderExerciseId, s => s.Exercise);
        // Preserva a ordem original de ExerciseSelectionEngine.Select (variedade/prioridade de
        // exibição) - o ladder de tempo só decide QUAIS exercícios sobrevivem/entram, nunca a
        // ordem. Candidatos extras adicionados por "added_volume" entram ao final, na ordem em
        // que o ladder os escolheu.
        var finalSelectedExercises = selectedExercises
            .Where(exercise => timeBudgetByExerciseId.ContainsKey(exercise.ProviderExerciseId))
            .ToList();
        var originalIds = finalSelectedExercises.Select(e => e.ProviderExerciseId).ToHashSet(StringComparer.Ordinal);
        foreach (var item in timeBudgetResolution.Items)
        {
            if (originalIds.Contains(item.ExerciseId)) continue;
            if (byProviderId.TryGetValue(item.ExerciseId, out var addedExercise))
                finalSelectedExercises.Add(addedExercise);
        }
        selectedExercises = finalSelectedExercises;

        int SetsForFinal(ExerciseCatalog exercise) =>
            timeBudgetByExerciseId.TryGetValue(exercise.ProviderExerciseId, out var item) ? item.Sets : SetsFor(exercise);
        int RestSecondsForFinal(ExerciseCatalog exercise) =>
            timeBudgetByExerciseId.TryGetValue(exercise.ProviderExerciseId, out var item) ? item.RestSeconds : prescription.RestSeconds;

        var catalogWorkout = new
        {
            title = "",
            description = LocalizedStrings.CatalogDescription(language),
            durationMinutes = profile.AvailableMinutesPerWorkout,
            // US-240 5.3: rótulo do dia/aviso de recuperação exibidos no Flutter (pre_quest_page).
            resolvedProgramKey,
            resolvedDayKey,
            dayLabelI18nKey = string.IsNullOrEmpty(blueprint.DayLabelI18nKey) ? null : blueprint.DayLabelI18nKey,
            splitMapVersion,
            hasMuscleGroupInRecovery,
            // US-242: orçamento de tempo determinístico aplicado a esta quest.
            estimatedDurationSeconds = timeBudgetResolution.EstimatedDurationSeconds,
            timeBudgetSeconds = timeBudgetResolution.TimeBudgetSeconds,
            timeAdjustmentApplied = timeBudgetResolution.TimeAdjustmentApplied,
            isMicroQuest = timeBudgetResolution.IsMicroQuest,
            workoutTimeModelVersion = WorkoutTimeModel.Version,
            // US-241: avisos de progressão semanal exibidos no Flutter (pre_quest_page).
            deloadWeek = weeklyPlan?.DeloadWeek ?? false,
            progressionDecision = weeklyPlan?.Decision,
            recalibratedFromProfileChange = weeklyPlan?.RecalibratedFromProfileChange ?? false,
            exercises = selectedExercises.Select(exercise => new
            {
                id = exercise.ProviderExerciseId,
                name = exercise.NamePtBr,
                description = exercise.DescriptionPtBr,
                instructions = exercise.InstructionsPtBr,
                // US-041: dicas do exercicio (TipsPtBr) tambem precisam chegar na tela de
                // pre-quest - antes desta correcao, eram geradas mas nunca serializadas.
                tips = exercise.TipsPtBr,
                sets = SetsForFinal(exercise),
                repsMin = prescription.RepsMin,
                repsMax = prescription.RepsMax,
                restSeconds = RestSecondsForFinal(exercise),
                targetRpe = TargetRpeFor(exercise),
                videoUrl = exercise.VideoUrl,
                imageUrl = exercise.ImageUrl,
                gifUrl = exercise.GifUrl,
                attributeContribution = exercise.AttributeContribution is null ? null : new
                {
                    primaryAttribute = exercise.AttributeContribution.PrimaryAttribute,
                    strengthXp = exercise.AttributeContribution.StrengthXp,
                    agilityXp = exercise.AttributeContribution.AgilityXp,
                    enduranceXp = exercise.AttributeContribution.EnduranceXp,
                    vitalityXp = exercise.AttributeContribution.VitalityXp,
                    focusXp = exercise.AttributeContribution.FocusXp,
                    wisdomXp = exercise.AttributeContribution.WisdomXp
                }
            })
        };

        var workoutJson = JsonSerializer.Serialize(catalogWorkout, CamelCaseJsonOptions);
        return new WorkoutGenerationResult(
            workoutJson,
            IsPersonalized: true,
            GenerationMethod: "catalog_rules",
            AppliedFiltersJson: appliedFiltersJson,
            ResolvedProgramKey: resolvedProgramKey,
            ResolvedDayKey: resolvedDayKey,
            ResolvedDayIndex: resolvedDayIndex,
            SplitMapVersion: splitMapVersion,
            DailyWorkoutBlueprintJson: blueprintJson,
            EstimatedDurationSeconds: timeBudgetResolution.EstimatedDurationSeconds,
            TimeBudgetSeconds: timeBudgetResolution.TimeBudgetSeconds,
            TimeAdjustmentApplied: timeBudgetResolution.TimeAdjustmentApplied,
            WorkoutTimeModelVersion: WorkoutTimeModel.Version,
            WeeklyProgressionPlanJson: weeklyPlan is null ? null : JsonSerializer.Serialize(weeklyPlan, CamelCaseJsonOptions));
    }

    /// US-240: restringe o catálogo aprovado ao subconjunto coerente com o alvo do
    /// dia (grupo E padrão, RN-002) antes do filtro de segurança. Quando o blueprint
    /// aciona fallback (RN-007/US-046) ou não há split configurado (RN-009), mantém
    /// o catálogo completo - comportamento anterior a esta US, sem regressão.
    private static IReadOnlyList<ExerciseCatalog> ApplyBlueprintCoherenceFilter(
        IReadOnlyList<ExerciseCatalog> approvedExercises, DailyWorkoutBlueprint blueprint)
    {
        if (blueprint.FallbackUsed || blueprint.TargetGroups.Count == 0)
            return approvedExercises;

        var targetGroups = blueprint.TargetGroups.Select(g => g.MuscleGroup).ToHashSet(StringComparer.Ordinal);
        var targetPatterns = blueprint.TargetPatterns.ToHashSet(StringComparer.Ordinal);
        var avoidFamiliesByGroup = blueprint.TargetGroups
            .Where(g => g.AvoidMovementFamilies.Count > 0)
            .ToDictionary(
                g => g.MuscleGroup,
                g => g.AvoidMovementFamilies.ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);

        var coherent = approvedExercises
            .Where(e => e.PrimaryMuscleGroups.Any(targetGroups.Contains) && targetPatterns.Contains(e.MovementPattern))
            .ToList();

        var withoutRecentlyUsedFamilies = coherent
            .Where(e =>
            {
                var primaryGroup = e.PrimaryMuscleGroups.FirstOrDefault();
                return primaryGroup is null
                    || !avoidFamiliesByGroup.TryGetValue(primaryGroup, out var avoidFamilies)
                    || !avoidFamilies.Contains(e.MovementFamily);
            })
            .ToList();

        return withoutRecentlyUsedFamilies.Count > 0 ? withoutRecentlyUsedFamilies : coherent;
    }

    /// US-240 RN-008/US-049: campos de auditoria do dia resolvido/blueprint, nulos
    /// quando o programa não tem split clássico configurado (sentinela "N/A"/"n/a").
    private static (string? ProgramKey, string? DayKey, int? DayIndex, string? SplitMapVersion, string BlueprintJson)
        BuildBlueprintAuditFields(DailyWorkoutBlueprint blueprint)
    {
        var hasResolvedDay = blueprint.ResolvedDayKey != "N/A";
        return (
            blueprint.ProgramKey,
            hasResolvedDay ? blueprint.ResolvedDayKey : null,
            hasResolvedDay ? blueprint.ResolvedDayIndex : null,
            hasResolvedDay ? blueprint.SplitMapVersion : null,
            JsonSerializer.Serialize(blueprint, CamelCaseJsonOptions));
    }

    /// US-230: mesma pipeline de segurança/pontuação da geração original,
    /// mas escolhendo apenas 1 substituto elegível fora da quest atual.
    public async Task<QuestExerciseSeed?> SelectSubstituteExerciseAsync(
        string fitnessProfileJson,
        IReadOnlyCollection<string> excludeProviderExerciseIds,
        CancellationToken cancellationToken = default)
    {
        var profile = ParseProfile(fitnessProfileJson);

        var approvedExercises = await exerciseCatalogRepository.ListApprovedForWorkoutGenerationAsync(cancellationToken);

        var context = new ExerciseSafetyContext(
            EffectiveExperienceLevel: profile.EffectiveExperienceLevel,
            EquipmentAvailable: profile.EquipmentAvailable,
            AvailableMinutesPerWorkout: profile.AvailableMinutesPerWorkout,
            PhysicalLimitations: profile.PhysicalLimitations,
            PhysicalPains: profile.PhysicalPains,
            Bmi: profile.Bmi);

        var eligibleExercises = ExerciseSafetyFilter.Apply(approvedExercises, context)
            .Where(e => !excludeProviderExerciseIds.Contains(e.ProviderExerciseId))
            .ToList();

        if (eligibleExercises.Count == 0)
            return null;

        var scoringContext = new ExerciseScoringContext(
            Goal: profile.Goal,
            EffectiveExperienceLevel: profile.EffectiveExperienceLevel,
            AvailableMinutesPerWorkout: profile.AvailableMinutesPerWorkout,
            UserAttributes: profile.UserAttributes);

        var best = ExerciseScoringEngine.Score(eligibleExercises, scoringContext).First().Exercise;
        var prescription = ExercisePrescriptionEngine.Prescribe(profile.EffectiveExperienceLevel, profile.Goal);

        return BuildSeed(best, prescription);
    }

    private static QuestExerciseSeed BuildSeed(ExerciseCatalog exercise, ExercisePrescription prescription)
    {
        var attr = exercise.AttributeContribution;
        int strengthXp = 0, agilityXp = 0, enduranceXp = 0, vitalityXp = 0, focusXp = 0, wisdomXp = 0;
        long baseXp;

        if (attr is not null)
        {
            strengthXp = attr.StrengthXp;
            agilityXp = attr.AgilityXp;
            enduranceXp = attr.EnduranceXp;
            vitalityXp = attr.VitalityXp;
            focusXp = attr.FocusXp;
            wisdomXp = attr.WisdomXp;
            baseXp = strengthXp + agilityXp + enduranceXp + vitalityXp + focusXp + wisdomXp;
        }
        else
        {
            wisdomXp = 1;
            baseXp = 1;
        }

        var xpReward = Math.Max(1, (long)Math.Round(baseXp * prescription.Sets * prescription.RepsMin / 10.0));

        return new QuestExerciseSeed(
            Name: exercise.NamePtBr,
            ExerciseCatalogProviderId: exercise.ProviderExerciseId,
            Sets: prescription.Sets,
            RepsMin: prescription.RepsMin,
            RepsMax: prescription.RepsMax,
            RestSeconds: prescription.RestSeconds,
            TargetRpe: prescription.TargetRpe,
            VideoUrl: exercise.VideoUrl ?? exercise.GifUrl ?? exercise.ImageUrl,
            XpReward: xpReward,
            StrengthXp: strengthXp,
            AgilityXp: agilityXp,
            EnduranceXp: enduranceXp,
            VitalityXp: vitalityXp,
            FocusXp: focusXp,
            WisdomXp: wisdomXp);
    }

    private static string BuildFallbackWorkoutJson(
        string language,
        string effectiveExperienceLevel,
        string? goal,
        int availableMinutesPerWorkout,
        int exerciseCount,
        string? resolvedProgramKey = null,
        string? resolvedDayKey = null,
        string? dayLabelI18nKey = null,
        string? splitMapVersion = null,
        bool hasMuscleGroupInRecovery = false,
        WeeklyProgressionPlan? weeklyPlan = null)
    {
        var prescription = ExercisePrescriptionEngine.Prescribe(effectiveExperienceLevel, goal);
        var names = LocalizedStrings.FallbackExerciseNames(language);
        var fallbackExercises = names
            .Select(name => new { name, sets = prescription.Sets, repsMin = prescription.RepsMin, repsMax = prescription.RepsMax, restSeconds = prescription.RestSeconds, targetRpe = prescription.TargetRpe })
            .Take(exerciseCount);

        var fallback = new
        {
            title = "",
            description = LocalizedStrings.FallbackDescription(language),
            durationMinutes = availableMinutesPerWorkout,
            resolvedProgramKey,
            resolvedDayKey,
            dayLabelI18nKey = string.IsNullOrEmpty(dayLabelI18nKey) ? null : dayLabelI18nKey,
            splitMapVersion,
            hasMuscleGroupInRecovery,
            // US-242: fallback usa orçamento de tempo conservador (sem ladder de ajuste).
            estimatedDurationSeconds = availableMinutesPerWorkout * 60,
            timeBudgetSeconds = availableMinutesPerWorkout * 60,
            timeAdjustmentApplied = availableMinutesPerWorkout <= WorkoutTimeModel.MicroQuestThresholdMinutes ? "micro_quest" : "none",
            isMicroQuest = availableMinutesPerWorkout <= WorkoutTimeModel.MicroQuestThresholdMinutes,
            workoutTimeModelVersion = WorkoutTimeModel.Version,
            // US-241: avisos de progressão semanal exibidos no Flutter (pre_quest_page).
            deloadWeek = weeklyPlan?.DeloadWeek ?? false,
            progressionDecision = weeklyPlan?.Decision,
            recalibratedFromProfileChange = weeklyPlan?.RecalibratedFromProfileChange ?? false,
            exercises = fallbackExercises
        };

        return JsonSerializer.Serialize(fallback, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static ParsedFitnessProfile ParseProfile(string fitnessProfileJson)
    {
        try
        {
            using var document = JsonDocument.Parse(fitnessProfileJson);
            var root = document.RootElement;

            var goal = ReadString(root, "goal");
            var experienceLevel = ReadString(root, "experienceLevel") ?? "sedentary";
            var trainingDuration = ReadString(root, "trainingDuration");
            // US-150: effectiveExperienceLevel reconcilia experienceLevel x trainingDuration;
            // se o snapshot nao trouxer o valor calculado, recalcula com os dados brutos.
            var effectiveExperienceLevel = ReadString(root, "effectiveExperienceLevel")
                ?? ExperienceLevelCalculator.CalculateEffectiveLevel(experienceLevel, trainingDuration);
            var availableMinutesPerWorkout = ReadPositiveInt(root, "availableMinutesPerWorkout") ?? 30;
            var equipmentAvailable = ReadStringList(root, "equipmentAvailable");
            var physicalLimitations = ReadStringList(root, "physicalLimitations")
                .Except(NoLimitationSentinels, StringComparer.OrdinalIgnoreCase).ToList();
            var physicalPains = ReadStringList(root, "physicalPains")
                .Except(NoPainSentinels, StringComparer.OrdinalIgnoreCase).ToList();
            // US-040: physicalLimitations/physicalPains chegam aqui no vocabulario de onboarding
            // (CompleteOnboardingCommandValidator, ex. "knee_problem") - traduz para o vocabulario
            // de risco/articulacao do catalogo (ex. "knee_high_stress") antes de compor o
            // ExerciseSafetyContext, que e o que ExerciseSafetyFilter.HasConflict realmente compara.
            var translatedLimitations = OnboardingTagTranslator.TranslateLimitations(physicalLimitations).ToList();
            var translatedPains = OnboardingTagTranslator.TranslatePains(physicalPains).ToList();
            var bmi = ComputeBmi(ReadDecimal(root, "heightCm"), ReadDecimal(root, "weightKg"));
            var userAttributes = ReadUserAttributes(root);

            return new ParsedFitnessProfile(
                goal, experienceLevel, effectiveExperienceLevel, availableMinutesPerWorkout,
                equipmentAvailable, translatedLimitations, translatedPains, bmi, userAttributes);
        }
        catch (JsonException)
        {
            // Conservador (RN-EPIC-006-005): assume o perfil mais restritivo quando o payload e invalido.
            return new ParsedFitnessProfile(null, "sedentary", "sedentary", 30, [], [], [], null,
                new Dictionary<string, int>());
        }
    }

    private static decimal? ComputeBmi(decimal? heightCm, decimal? weightKg)
    {
        if (heightCm is not decimal height || weightKg is not decimal weight || height <= 0) return null;
        var heightMeters = height / 100m;
        return weight / (heightMeters * heightMeters);
    }

    private static string? ReadString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static decimal? ReadDecimal(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) && element.TryGetDecimal(out var value)
            ? value
            : null;

    private static int? ReadPositiveInt(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) &&
        element.TryGetInt32(out var value) && value > 0
            ? value
            : null;

    private static List<string> ReadStringList(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.Array
            ? element.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .ToList()
            : [];

    private static IReadOnlyDictionary<string, int> ReadUserAttributes(JsonElement root)
    {
        if (!root.TryGetProperty("userAttributes", out var el) || el.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, int>();

        var attrs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Value.TryGetInt32(out var val))
                attrs[prop.Name] = val;
        }
        return attrs;
    }

    private static int ResolveExerciseCount(int availableMinutesPerWorkout) =>
        Math.Clamp((int)Math.Ceiling(availableMinutesPerWorkout / 10.0), 1, 4);

    /// US-241: aplica no máximo 1 eixo de ajuste (RN-003) sobre a prescrição base
    /// (US-153). O teto de séries do blueprint (US-239/US-240) continua soberano —
    /// aplicado depois, na composição por exercício (RN-009).
    private static ExercisePrescription ApplyWeeklyProgressionPlan(
        ExercisePrescription prescription, WeeklyProgressionPlan? plan)
    {
        if (plan is null) return prescription;

        var sets = Math.Max(1, prescription.Sets + plan.VolumeSetsDelta);
        var restSeconds = Math.Max(20, prescription.RestSeconds + plan.RestSecondsDelta);
        var repsMin = plan.Axis == "reps" && plan.Decision == "progress" ? prescription.RepsMin + 1 : prescription.RepsMin;
        var repsMax = plan.Axis == "reps" && plan.Decision == "progress" && prescription.RepsMax is int max
            ? max + 1
            : prescription.RepsMax;

        if (plan.DeloadWeek)
        {
            sets = Math.Max(1, (int)Math.Round(prescription.Sets * 0.5, MidpointRounding.AwayFromZero));
            restSeconds = prescription.RestSeconds;
            repsMin = prescription.RepsMin;
            repsMax = prescription.RepsMax;
        }

        return prescription with { Sets = sets, RepsMin = repsMin, RepsMax = repsMax, RestSeconds = restSeconds };
    }

    private record ParsedFitnessProfile(
        string? Goal,
        string ExperienceLevel,
        string EffectiveExperienceLevel,
        int AvailableMinutesPerWorkout,
        List<string> EquipmentAvailable,
        List<string> PhysicalLimitations,
        List<string> PhysicalPains,
        decimal? Bmi,
        IReadOnlyDictionary<string, int> UserAttributes);
}
