using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Commands.ConfirmDailyQuest;

public class ConfirmDailyQuestCommandHandler(
    IQuestRepository questRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<ConfirmDailyQuestCommand, QuestResponse>
{
    public async Task<QuestResponse> Handle(
        ConfirmDailyQuestCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdAsync(request.QuestId, cancellationToken)
            ?? throw new NotFoundException("Quest", request.QuestId);

        if (quest.UserId != userId)
            throw new UnauthorizedException("QUEST_NOT_OWNED", "Quest nao pertence ao usuario atual.");

        // RN-007/RN-008 (US-042): idempotente - confirmar mais de uma vez nao tem efeito colateral.
        quest.Confirm(dateTimeService.UtcNow);
        questRepository.Update(quest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return QuestResponseMapper.ToResponse(quest);
    }
}
