using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Quests.Commands.StartQuest;

public class StartQuestCommandHandler(
    IQuestRepository questRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<StartQuestCommandHandler> logger) : IRequestHandler<StartQuestCommand, StartQuestResponse>
{
    private static readonly HashSet<string> ValidTypes = ["daily", "dungeon", "raid"];

    public async Task<StartQuestResponse> Handle(
        StartQuestCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdAsync(request.QuestId, cancellationToken);

        // RN-006/CA-002: acesso e validado pelo ActiveAccessMiddleware antes de chegar aqui.
        // Quest inexistente ou de outro usuario: nao revelar a existencia -> 404.
        if (quest is null || quest.UserId != userId)
            throw new NotFoundException("Quest", request.QuestId);

        // RN-004: quest deve possuir um type valido para ser executavel.
        if (!ValidTypes.Contains(quest.Type))
            throw new ConflictException("INVALID_QUEST_TYPE", "Tipo de quest invalido para iniciar execucao.");

        // RN-003: quest ja iniciada e idempotente - retorna o registro existente sem novo carimbo.
        if (quest.Status != "in_progress")
        {
            // RN-002: apenas quest pendente/pronta pode ser iniciada.
            if (quest.Status != "pending")
                throw new ConflictException("QUEST_NOT_STARTABLE", "Quest nao esta em um estado iniciavel.");

            var seeds = QuestExerciseSeedMapper.ParseSeeds(quest.WorkoutJson);
            quest.Start(dateTimeService.UtcNow, seeds);
            await questRepository.AddExercisesAsync(quest.Exercises, cancellationToken);
            questRepository.UpdateRoot(quest);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "quest_started questId={QuestId} userId={UserId} questType={QuestType}",
                quest.Id, userId, quest.Type);

            if (quest.Type == "dungeon")
            {
                logger.LogInformation(
                    "dungeon_activated questId={QuestId} userId={UserId} questType={QuestType} source=quest_start status=activated result=success",
                    quest.Id, userId, quest.Type);
            }
        }

        return new StartQuestResponse(quest.Id, quest.Type, quest.Status, quest.StartedAtUtc!.Value);
    }
}
