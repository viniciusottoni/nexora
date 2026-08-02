namespace Awaken.Application.Progression.Services;

public interface IDailyQuestPenaltyService
{
    Task<DailyPenaltyRolloverSummary> ApplyForQuestDateAsync(
        DateTime questDateUtc,
        CancellationToken cancellationToken = default);

    Task<DailyPenaltyRolloverSummary> ApplyForUserBeforeDateAsync(
        Guid userId,
        DateTime beforeQuestDateUtc,
        CancellationToken cancellationToken = default);
}

public record DailyPenaltyRolloverSummary(int MissedDailiesChecked, int PenaltiesApplied);
