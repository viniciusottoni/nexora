using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Commands.ResumeQuest;

/// US-059: retoma a execucao de uma quest pausada (RN-002), sem conceder XP (RN-004).
public class ResumeQuestCommandHandler(
    IQuestRepository questRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<ResumeQuestCommand, ResumeQuestResponse>
{
    public async Task<ResumeQuestResponse> Handle(
        ResumeQuestCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdAsync(request.QuestId, cancellationToken);

        // Quest inexistente ou de outro usuario: nao revelar a existencia -> 404.
        if (quest is null || quest.UserId != userId)
            throw new NotFoundException("Quest", request.QuestId);

        // Retomar uma quest ja em andamento e idempotente - nao reescreve ResumedAtUtc.
        if (quest.Status != "in_progress")
        {
            // RN-002: apenas quest pausada pode ser retomada.
            if (quest.Status != "paused")
                throw new ConflictException("QUEST_NOT_RESUMABLE", "Quest nao esta em um estado retomavel.");

            quest.Resume(dateTimeService.UtcNow);
            questRepository.Update(quest);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new ResumeQuestResponse(quest.Id, quest.Type, quest.Status, quest.ResumedAtUtc!.Value);
    }
}
