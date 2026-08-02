using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetQuestPreview;

public class GetQuestPreviewQueryHandler(
    IQuestRepository questRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetQuestPreviewQuery, QuestPreviewResponse>
{
    public async Task<QuestPreviewResponse> Handle(
        GetQuestPreviewQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdAsync(request.QuestId, cancellationToken)
            ?? throw new NotFoundException("Quest", request.QuestId);

        if (quest.UserId != userId)
            throw new UnauthorizedException("QUEST_NOT_OWNED", "Quest nao pertence ao usuario atual.");

        return QuestResponseMapper.ToPreviewResponse(quest);
    }
}
