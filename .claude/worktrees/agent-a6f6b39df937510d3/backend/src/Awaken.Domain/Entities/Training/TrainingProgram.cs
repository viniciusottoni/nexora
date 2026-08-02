using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Training;

/// US-231: catalogo de programas de treino com restricao por rank minimo.
public class TrainingProgram : BaseEntity
{
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public string MinimumRank { get; private set; } = "E+";

    /// Dias de divisao semanal do programa; null = adaptativo (ex.: "System").
    public int? SplitDays { get; private set; }

    public bool IsActive { get; private set; } = true;

    private TrainingProgram() { }

    public static TrainingProgram Create(
        string key,
        string name,
        string? description,
        string? category,
        string minimumRank,
        int? splitDays,
        DateTime utcNow)
    {
        return new TrainingProgram
        {
            Key = key,
            Name = name,
            Description = description,
            Category = category,
            MinimumRank = minimumRank,
            SplitDays = splitDays,
            IsActive = true,
            CreatedAtUtc = utcNow,
        };
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAtUtc = utcNow;
    }
}
