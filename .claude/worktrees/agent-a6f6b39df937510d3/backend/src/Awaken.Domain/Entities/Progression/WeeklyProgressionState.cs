using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Progression;

/// US-241 §14: estado de progressão semanal por usuário. `VolumeSetsDelta` é o
/// número de séries acrescidas sobre o baseline da prescrição (US-153), capado
/// pelo teto recuperável do blueprint (US-239/US-240) no momento de aplicar,
/// não aqui (este estado não conhece o blueprint).
public class WeeklyProgressionState : BaseEntity
{
    public const int MaxConsecutiveWeeksBeforeDeload = 5; // ~4-6 semanas (RN-005)
    public const int MaxConsecutiveHardWeeksBeforeDeload = 2;
    public const int MaxVolumeSetsDelta = 4;

    public Guid UserId { get; private set; }
    public DateOnly WeekAnchorDate { get; private set; }
    public int MesocycleWeekIndex { get; private set; } = 1;
    public string? ProfileSnapshotHash { get; private set; }
    public int ConsecutiveEasyWeeks { get; private set; }
    public int ConsecutiveHardWeeks { get; private set; }
    public bool DeloadDue { get; private set; }
    public string LastDecision { get; private set; } = "hold";
    public string? LastAxis { get; private set; }
    public int VolumeSetsDelta { get; private set; }
    public int RpeDelta { get; private set; }
    public int RestSecondsDelta { get; private set; }

    private WeeklyProgressionState() { }

    public static WeeklyProgressionState CreateInitial(
        Guid userId, DateOnly weekAnchorDate, string? profileSnapshotHash, DateTime utcNow) =>
        new()
        {
            UserId = userId,
            WeekAnchorDate = weekAnchorDate,
            ProfileSnapshotHash = profileSnapshotHash,
            UpdatedAtUtc = utcNow,
        };

    /// US-241 RN-001/RN-006: aplica o resultado da reavaliação semanal (nova
    /// semana OU mudança de perfil), avançando o mesociclo quando de fato é
    /// uma nova semana (não em recalibração pura por mudança de perfil).
    public void ApplyWeeklyDecision(
        DateOnly newWeekAnchorDate,
        bool isNewWeek,
        string decision,
        string? axis,
        int volumeSetsDelta,
        int rpeDelta,
        int restSecondsDelta,
        int consecutiveEasyWeeks,
        int consecutiveHardWeeks,
        bool deloadApplied,
        string? profileSnapshotHash,
        DateTime utcNow)
    {
        WeekAnchorDate = newWeekAnchorDate;
        if (isNewWeek)
            MesocycleWeekIndex = deloadApplied ? 1 : MesocycleWeekIndex + 1;

        LastDecision = decision;
        LastAxis = axis;
        VolumeSetsDelta = volumeSetsDelta;
        RpeDelta = rpeDelta;
        RestSecondsDelta = restSecondsDelta;
        ConsecutiveEasyWeeks = consecutiveEasyWeeks;
        ConsecutiveHardWeeks = consecutiveHardWeeks;
        DeloadDue = deloadApplied;
        ProfileSnapshotHash = profileSnapshotHash;
        UpdatedAtUtc = utcNow;
    }
}
