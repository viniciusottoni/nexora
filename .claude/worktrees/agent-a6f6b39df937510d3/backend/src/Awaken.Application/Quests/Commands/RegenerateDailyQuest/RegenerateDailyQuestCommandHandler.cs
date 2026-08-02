using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using Awaken.Domain.Services.Quests;
using MediatR;

namespace Awaken.Application.Quests.Commands.RegenerateDailyQuest;

public class RegenerateDailyQuestCommandHandler(
    IQuestRepository questRepository,
    IInventoryRepository inventoryRepository,
    IQuestRegenerationService questRegenerationService,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUserDateService userDateService,
    IUnitOfWork unitOfWork) : IRequestHandler<RegenerateDailyQuestCommand, QuestResponse>
{
    private const string DailyType = "daily";

    public async Task<QuestResponse> Handle(
        RegenerateDailyQuestCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var questDateUtc = DateTime.SpecifyKind(
            userDateService.TodayLocal.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var quest = await questRepository.GetByUserIdAndDateAsync(
            userId, DailyType, questDateUtc, cancellationToken)
            ?? throw new NotFoundException("Quest", userId);

        // RN-001/RN-003: regeneracoes gratuitas tem limite diario; alem dele,
        // so e possivel regenerar consumindo o "Pergaminho da Reforja".
        var utcNow = dateTimeService.UtcNow;
        var viaReforgeScroll = false;
        if (quest.RegenerationCount >= QuestRegenerationPolicy.DailyFreeLimit)
        {
            if (!request.UseReforgeScroll)
                throw new ConflictException("REGENERATION_LIMIT_REACHED", "Limite diario de regeneracoes atingido.");

            var item = await inventoryRepository.GetByUserIdAndItemKeyAsync(
                userId, ItemKeys.ReforgeScroll, cancellationToken);
            if (item is null || item.Quantity <= 0)
                throw new ConflictException("REFORGE_SCROLL_NOT_AVAILABLE", "Pergaminho da Reforja indisponivel.");

            item.ConsumeOne(utcNow);
            inventoryRepository.Update(item);
            viaReforgeScroll = true;
        }

        // US-230: mecanica de regeneracao (perfil/geracao/auditoria) extraida
        // para IQuestRegenerationService, compartilhada com ReforgeScrollEffectHandler.
        var regeneratedQuest = await questRegenerationService.RegenerateAsync(
            userId, viaReforgeScroll, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return QuestResponseMapper.ToResponse(regeneratedQuest);
    }
}
