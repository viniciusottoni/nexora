using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Progression.Services;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetDailyQuest;

public class GetDailyQuestQueryHandler(
    IQuestRepository questRepository,
    ICurrentUserService currentUserService,
    IUserDateService userDateService,
    IDailyQuestPenaltyService dailyQuestPenaltyService) : IRequestHandler<GetDailyQuestQuery, QuestResponse>
{
    private const string DailyType = "daily";

    public async Task<QuestResponse> Handle(GetDailyQuestQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var questDateUtc = DateTime.SpecifyKind(
            userDateService.TodayLocal.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        await dailyQuestPenaltyService.ApplyForUserBeforeDateAsync(userId, questDateUtc, cancellationToken);

        var quest = await questRepository.GetByUserIdAndDateAsync(
            userId, DailyType, questDateUtc, cancellationToken)
            ?? throw new NotFoundException("Quest", questDateUtc);

        return QuestResponseMapper.ToResponse(quest);
    }
}
