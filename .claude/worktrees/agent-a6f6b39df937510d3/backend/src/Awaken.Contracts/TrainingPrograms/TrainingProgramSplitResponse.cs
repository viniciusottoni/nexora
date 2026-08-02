namespace Awaken.Contracts.TrainingPrograms;

public record TrainingProgramSplitResponse(
    string ProgramKey,
    string SplitMapVersion,
    IReadOnlyList<TrainingSplitDayResponse> Days);

public record TrainingSplitDayResponse(
    string DayKey,
    string Role,
    string LabelI18nKey,
    IReadOnlyList<string> TargetMuscleGroups,
    IReadOnlyList<string> TargetMovementPatterns,
    bool AllowsCoreFinisher);
