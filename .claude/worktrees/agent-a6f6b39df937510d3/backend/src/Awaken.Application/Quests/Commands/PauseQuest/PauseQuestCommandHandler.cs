using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Commands.PauseQuest;

/// US-059: pausa a execucao de uma quest em andamento (RN-001), sem conceder XP (RN-003).
public class PauseQuestCommandHandler(
    IQuestRepository questRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<PauseQuestCommand, PauseQuestResponse>
{
    public async Task<PauseQuestResponse> Handle(
        PauseQuestCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdAsync(request.QuestId, cancellationToken);

        // Quest inexistente ou de outro usuario: nao revelar a existencia -> 404.
        if (quest is null || quest.UserId != userId)
            throw new NotFoundException("Quest", request.QuestId);

        // RN-003: pausar uma quest ja pausada e idempotente - nao reescreve PausedAtUtc.
        if (quest.Status != "paused")
        {
            // RN-001/RN-006: apenas quest em andamento pode ser pausada.
            if (quest.Status != "in_progress")
                throw new ConflictException("QUEST_NOT_PAUSABLE", "Quest nao esta em um estado pausavel.");

            quest.Pause(dateTimeService.UtcNow);
            questRepository.Update(quest);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new PauseQuestResponse(quest.Id, quest.Type, quest.Status, quest.PausedAtUtc!.Value);
    }
}
