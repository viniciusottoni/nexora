using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Training;

/// US-237: fonte de verdade determinística sobre o que cada dia (letra) de
/// cada programa clássico treina — grupos musculares e padrões de movimento
/// alvo — ancorada nos enums de <see cref="MuscleGroups"/> e
/// <see cref="MovementPatterns"/> (RN-005) e coerente com a descrição
/// publicada do programa na US-231.
public class TrainingProgramSplit : BaseEntity
{
    private static readonly IReadOnlyDictionary<string, int> ExpectedDayCounts = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [TrainingProgramKeys.FullBody] = 1,
        [TrainingProgramKeys.Ab] = 2,
        [TrainingProgramKeys.Abc] = 3,
        [TrainingProgramKeys.Abcd] = 4,
        [TrainingProgramKeys.Abcde] = 5,
    };

    public string ProgramKey { get; private set; } = string.Empty;
    public string SplitMapVersion { get; private set; } = string.Empty;
    public int DayCount { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<TrainingSplitDay> _days = [];
    public IReadOnlyList<TrainingSplitDay> Days => _days.AsReadOnly();

    private TrainingProgramSplit() { }

    /// Cria um split map validado. RN-002: precisa de ao menos 1 dia, e cada
    /// dia precisa de ao menos 1 grupo e 1 padrão-alvo. RN-005: grupos e
    /// padrões devem pertencer aos enums já existentes no catálogo — valor
    /// fora do enum bloqueia a criação (e, portanto, o seed).
    public static TrainingProgramSplit Create(string programKey, string splitMapVersion, IEnumerable<TrainingSplitDaySeed> days)
    {
        var daysList = days.ToList();
        if (daysList.Count == 0)
            throw new InvalidOperationException($"Split map de '{programKey}' precisa de ao menos 1 dia."); // RN-002
        if (ExpectedDayCounts.TryGetValue(programKey, out var expectedDayCount)
            && daysList.Count != expectedDayCount)
        {
            throw new InvalidOperationException(
                $"Split map de '{programKey}' precisa ter exatamente {expectedDayCount} dia(s)."); // RN-001
        }

        var split = new TrainingProgramSplit
        {
            ProgramKey = programKey,
            SplitMapVersion = splitMapVersion,
            DayCount = daysList.Count,
        };

        var index = 1;
        foreach (var day in daysList)
        {
            if (day.TargetMuscleGroups.Count == 0 || day.TargetMovementPatterns.Count == 0)
                throw new InvalidOperationException($"Dia '{day.DayKey}' precisa de ao menos 1 grupo e 1 padrão-alvo."); // RN-002

            foreach (var group in day.TargetMuscleGroups.Concat(day.SecondaryMuscleGroups))
                if (!MuscleGroups.IsValid(group))
                    throw new InvalidOperationException($"Grupo muscular inválido: '{group}'."); // RN-005

            foreach (var pattern in day.TargetMovementPatterns)
                if (!MovementPatterns.IsValid(pattern))
                    throw new InvalidOperationException($"Padrão de movimento inválido: '{pattern}'."); // RN-005

            split._days.Add(TrainingSplitDay.Create(index, day));
            index++;
        }

        return split;
    }
}

public record TrainingSplitDaySeed(
    string DayKey,
    string LabelI18nKey,
    string Role,
    IReadOnlyList<string> TargetMuscleGroups,
    IReadOnlyList<string> SecondaryMuscleGroups,
    IReadOnlyList<string> TargetMovementPatterns,
    bool AllowsCoreFinisher,
    int MinExercises,
    int MaxExercises);
