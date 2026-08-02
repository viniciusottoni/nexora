using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Onboarding;

public class UserWorkoutPreference : BaseEntity
{
    public Guid UserId { get; private set; }
    public string PreferredTrainingType { get; private set; } = null!;
    public string? PreferredProgramId { get; private set; }

    private UserWorkoutPreference() { }

    public static UserWorkoutPreference Create(
        Guid userId,
        string preferredTrainingType,
        string? preferredProgramId,
        DateTime utcNow)
    {
        return new UserWorkoutPreference
        {
            UserId = userId,
            PreferredTrainingType = preferredTrainingType,
            PreferredProgramId = preferredProgramId,
            UpdatedAtUtc = utcNow,
        };
    }

    public void UpdatePreference(
        string preferredTrainingType,
        string? preferredProgramId,
        DateTime utcNow)
    {
        PreferredTrainingType = preferredTrainingType;
        PreferredProgramId = preferredProgramId;
        UpdatedAtUtc = utcNow;
    }
}
