namespace Awaken.Contracts.TrainingPrograms;

public record TrainingProgramResponse(
    Guid Id,
    string ProgramKey,
    string Name,
    string? Description,
    string? Category,
    string MinimumRank,
    int? SplitDays,
    bool IsAvailable,
    bool IsSelected);
