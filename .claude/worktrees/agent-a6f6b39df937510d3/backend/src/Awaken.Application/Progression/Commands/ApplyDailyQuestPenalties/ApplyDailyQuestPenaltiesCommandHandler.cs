using Awaken.Application.Common.Interfaces;
using Awaken.Application.Progression.Services;
using MediatR;

namespace Awaken.Application.Progression.Commands.ApplyDailyQuestPenalties;

public class ApplyDailyQuestPenaltiesCommandHandler(
    IDailyQuestPenaltyService dailyQuestPenaltyService,
    IDateTimeService dateTimeService)
    : IRequestHandler<ApplyDailyQuestPenaltiesCommand, DailyPenaltyRolloverSummary>
{
    public async Task<DailyPenaltyRolloverSummary> Handle(
        ApplyDailyQuestPenaltiesCommand request,
        CancellationToken cancellationToken)
    {
        var yesterdayUtc = DateTime.SpecifyKind(
            dateTimeService.TodayUtc.AddDays(-1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        return await dailyQuestPenaltyService.ApplyForQuestDateAsync(yesterdayUtc, cancellationToken);
    }
}
