namespace Awaken.Domain.Services.Quests;

/// <summary>
/// US-242: fórmula de duração estimada por exercício/quest (§6.2/6.3) e ladder
/// determinístico de resolução de conflito objetivo/intensidade × tempo (§6.5).
/// Serviço de domínio puro — opera só sobre os itens que o chamador já
/// selecionou/pontuou (<see cref="ExerciseSelectionEngine"/>/
/// <see cref="ExercisePrescriptionEngine"/>), sem I/O.
/// </summary>
public static class TimeBudgetCalculator
{
    private static readonly string[] DensityEligibleGoals =
        ["lose_weight", "fat_loss", "improve_conditioning", "conditioning"];

    /// US-242 §6.2: transição + (séries × execução por série) + descanso ENTRE séries.
    public static int ExerciseTimeCostSeconds(int sets, int reps, int? plannedDurationSeconds, int restSeconds)
    {
        var execPerSet = plannedDurationSeconds ?? reps * WorkoutTimeModel.SecondsPerRep;
        return WorkoutTimeModel.TransitionSeconds + sets * execPerSet + Math.Max(0, sets - 1) * restSeconds;
    }

    /// US-242 §6.3: aquecimento + soma dos timeCost + finalização.
    public static int EstimateQuestDurationSeconds(
        IEnumerable<int> exerciseTimeCostsSeconds, int warmupSeconds, int cooldownSeconds) =>
        warmupSeconds + exerciseTimeCostsSeconds.Sum() + cooldownSeconds;

    /// US-242 §6.5: aplica a ordem determinística de ajuste quando estoura o tempo
    /// disponível, ou adiciona volume quando sobra tempo (RN-002/RN-003/RN-004).
    public static TimeBudgetResolution Resolve(TimeBudgetRequest request)
    {
        var level = request.EffectiveExperienceLevel;
        var isMicroQuestByThreshold = request.AvailableMinutesPerWorkout <= WorkoutTimeModel.MicroQuestThresholdMinutes;

        var warmup = isMicroQuestByThreshold ? WorkoutTimeModel.MicroQuestWarmupSeconds : WorkoutTimeModel.WarmupSecondsFor(level);
        var cooldown = isMicroQuestByThreshold ? 0 : WorkoutTimeModel.CooldownSeconds;
        var adjustment = isMicroQuestByThreshold ? "micro_quest" : "none";
        var densityApplied = false;

        var availableSeconds = request.AvailableMinutesPerWorkout * 60;
        var softMinSeconds = (int)Math.Floor(availableSeconds * WorkoutTimeModel.MinUtilization);

        // Ordenado por prioridade decrescente: o fim da lista é cortado primeiro (RN-002).
        var items = request.SelectedItems.OrderByDescending(i => i.PriorityScore).ToList();

        int Cost(TimeBudgetItem i) => ExerciseTimeCostSeconds(i.Sets, i.Reps, i.PlannedDurationSeconds, i.RestSeconds);
        int Total() => EstimateQuestDurationSeconds(items.Select(Cost), warmup, cooldown);

        if (Total() > availableSeconds)
        {
            // 1) Reduzir quantidade de exercícios (menor prioridade primeiro), até o mínimo do dia.
            while (Total() > availableSeconds && items.Count > request.MinExerciseCount)
            {
                items.RemoveAt(items.Count - 1);
                adjustment = "reduced_exercises";
            }

            // 2) Reduzir séries em direção a um piso de 2 (preserva ao menos 1 descanso
            // entre séries, pra a densidade do passo 3 ainda ter efeito - RN-004).
            void ReduceSetsToFloor(int floor)
            {
                while (Total() > availableSeconds && items.Any(i => i.Sets > floor))
                {
                    var index = items
                        .Select((item, idx) => (item, idx))
                        .Where(pair => pair.item.Sets > floor)
                        .OrderByDescending(pair => pair.item.Sets)
                        .ThenBy(pair => pair.item.PriorityScore)
                        .First().idx;
                    items[index] = items[index] with { Sets = items[index].Sets - 1 };
                    adjustment = "reduced_sets";
                }
            }

            ReduceSetsToFloor(2);

            // 3) Densidade (só condicionamento/perda de peso) — reduz descanso efetivo.
            if (Total() > availableSeconds && request.Goal is not null
                && DensityEligibleGoals.Contains(request.Goal, StringComparer.OrdinalIgnoreCase))
            {
                items = items.Select(i => i with { RestSeconds = Math.Max(15, i.RestSeconds / 2) }).ToList();
                densityApplied = true;
                adjustment = "density";
            }

            // 3b) Ainda estourando: reduzir o piso de séries até 1 (último recurso antes da micro quest).
            ReduceSetsToFloor(1);

            // 4) Micro quest — último recurso: aquecimento mínimo, sem finalização, mínimo de exercícios.
            if (Total() > availableSeconds)
            {
                warmup = WorkoutTimeModel.MicroQuestWarmupSeconds;
                cooldown = 0;
                while (Total() > availableSeconds && items.Count > 1)
                    items.RemoveAt(items.Count - 1);
                adjustment = "micro_quest";
            }
        }
        else if (Total() < softMinSeconds)
        {
            // RN-003: tempo sobrando — +séries (até o teto por exercício) primeiro, +1
            // exercício quando os já selecionados estiverem todos no teto, intercalando
            // sempre que necessário pra aproveitar melhor o tempo disponível.
            var addedSets = items.ToDictionary(i => i.ExerciseId, _ => 0);
            var extraPool = new List<TimeBudgetItem>(request.ExtraCandidates);
            var addedAny = false;
            var progressed = true;

            while (Total() < softMinSeconds && progressed)
            {
                progressed = false;

                var bumpCandidate = items
                    .Where(i => addedSets[i.ExerciseId] < request.MaxAddedSetsPerExercise)
                    .OrderByDescending(i => i.PriorityScore)
                    .FirstOrDefault();
                if (bumpCandidate is not null)
                {
                    var index = items.FindIndex(i => i.ExerciseId == bumpCandidate.ExerciseId);
                    var bumped = items[index] with { Sets = items[index].Sets + 1 };
                    var totalIfBumped = warmup + items.Select((i, idx) => idx == index ? Cost(bumped) : Cost(i)).Sum() + cooldown;
                    if (totalIfBumped <= availableSeconds)
                    {
                        items[index] = bumped;
                        addedSets[bumpCandidate.ExerciseId]++;
                        addedAny = true;
                        progressed = true;
                        continue;
                    }
                }

                var nextExtra = extraPool.OrderByDescending(i => i.PriorityScore).FirstOrDefault();
                if (nextExtra is not null)
                {
                    var totalIfAdded = warmup + items.Select(Cost).Sum() + Cost(nextExtra) + cooldown;
                    extraPool.Remove(nextExtra);
                    if (totalIfAdded <= availableSeconds)
                    {
                        items.Add(nextExtra);
                        addedSets[nextExtra.ExerciseId] = 0;
                        addedAny = true;
                        progressed = true;
                    }
                }
            }

            if (addedAny) adjustment = "added_volume";
        }

        var finalTotal = Total();
        return new TimeBudgetResolution(
            Items: items,
            WarmupSeconds: warmup,
            CooldownSeconds: cooldown,
            EstimatedDurationSeconds: finalTotal,
            TimeBudgetSeconds: availableSeconds,
            Utilization: availableSeconds == 0 ? 0 : Math.Round((double)finalTotal / availableSeconds, 4),
            TimeAdjustmentApplied: adjustment,
            IsMicroQuest: isMicroQuestByThreshold || adjustment == "micro_quest",
            DensityApplied: densityApplied);
    }
}

/// <param name="PriorityScore">Score estático do exercício (US-151, `ExerciseScore.StaticScore`)
/// — usado para decidir ordem de corte/acréscimo. Maior = mantido primeiro.</param>
public sealed record TimeBudgetItem(
    string ExerciseId,
    int Sets,
    int Reps,
    int? PlannedDurationSeconds,
    int RestSeconds,
    double PriorityScore);

public sealed record TimeBudgetRequest(
    IReadOnlyList<TimeBudgetItem> SelectedItems,
    IReadOnlyList<TimeBudgetItem> ExtraCandidates,
    string EffectiveExperienceLevel,
    string? Goal,
    int AvailableMinutesPerWorkout,
    int MinExerciseCount = 1,
    int MaxAddedSetsPerExercise = 2);

public sealed record TimeBudgetResolution(
    IReadOnlyList<TimeBudgetItem> Items,
    int WarmupSeconds,
    int CooldownSeconds,
    int EstimatedDurationSeconds,
    int TimeBudgetSeconds,
    double Utilization,
    string TimeAdjustmentApplied,
    bool IsMicroQuest,
    bool DensityApplied);
