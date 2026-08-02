using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Commands.CancelQuest;

/// US-060: cancela a execucao de uma quest em andamento ou pausada (RN-001),
/// impedindo conclusao posterior e recompensa completa (RN-002/RN-003).
public class CancelQuestCommandHandler(
    IQuestRepository questRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelQuestCommand, CancelQuestResponse>
{
    public async Task<CancelQuestResponse> Handle(
        CancelQuestCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdAsync(request.QuestId, cancellationToken);

        // Quest inexistente ou de outro usuario: nao revelar a existencia -> 404.
        if (quest is null || quest.UserId != userId)
            throw new NotFoundException("Quest", request.QuestId);

        // Cancelar uma quest ja cancelada e idempotente - nao reescreve CancelledAtUtc.
        if (quest.Status != "cancelled")
        {
            // RN-001: apenas quest em andamento ou pausada pode ser cancelada.
            if (quest.Status != "in_progress" && quest.Status != "paused")
                throw new ConflictException("QUEST_NOT_CANCELLABLE", "Quest nao esta em um estado cancelavel.");

            quest.Cancel(dateTimeService.UtcNow);
            questRepository.Update(quest);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new CancelQuestResponse(quest.Id, quest.Type, quest.Status, quest.CancelledAtUtc!.Value);
    }
}
