using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Training;

/// US-237: um dia (letra) dentro de um <see cref="TrainingProgramSplit"/>.
public class TrainingSplitDay : BaseEntity
{
    public Guid TrainingProgramSplitId { get; private set; }
    public int DayIndex { get; private set; }
    public string DayKey { get; private set; } = string.Empty;
    public string LabelI18nKey { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public List<string> TargetMuscleGroups { get; private set; } = [];
    public List<string> SecondaryMuscleGroups { get; private set; } = [];
    public List<string> TargetMovementPatterns { get; private set; } = [];
    public bool AllowsCoreFinisher { get; private set; }
    public int MinExercises { get; private set; }
    public int MaxExercises { get; private set; }

    private TrainingSplitDay() { }

    internal static TrainingSplitDay Create(int dayIndex, TrainingSplitDaySeed seed) => new()
    {
        DayIndex = dayIndex,
        DayKey = seed.DayKey,
        LabelI18nKey = seed.LabelI18nKey,
        Role = seed.Role,
        TargetMuscleGroups = seed.TargetMuscleGroups.ToList(),
        SecondaryMuscleGroups = seed.SecondaryMuscleGroups.ToList(),
        TargetMovementPatterns = seed.TargetMovementPatterns.ToList(),
        AllowsCoreFinisher = seed.AllowsCoreFinisher,
        MinExercises = seed.MinExercises,
        MaxExercises = seed.MaxExercises,
    };
}
