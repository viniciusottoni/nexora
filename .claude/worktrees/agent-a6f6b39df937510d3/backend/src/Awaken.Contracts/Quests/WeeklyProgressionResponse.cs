namespace Awaken.Contracts.Quests;

/// US-241 §18: plano de progressão semanal vigente para o usuário atual.
public record WeeklyProgressionResponse(
    string WeekAnchorDate,
    int MesocycleWeekIndex,
    bool DeloadWeek,
    bool RecalibratedFromProfileChange,
    string Rank,
    string Decision,
    string? Axis,
    int VolumeSetsDelta,
    int RpeDelta,
    int RestSecondsDelta);
