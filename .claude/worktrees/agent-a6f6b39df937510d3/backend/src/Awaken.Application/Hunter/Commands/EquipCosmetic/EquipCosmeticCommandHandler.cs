using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Hunter.Commands.EquipCosmetic;

public class EquipCosmeticCommandHandler(
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IHunterProgressionRepository hunterProgressionRepository,
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<EquipCosmeticCommand, Unit>
{
    private const string FramePrefix = "frame_rank_";
    private const string AuraPrefix = "aura_";
    private const string BackgroundPrefix = "background_";

    public async Task<Unit> Handle(EquipCosmeticCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;

        if (request.ItemKey is not null)
        {
            // RN-001: o item precisa pertencer a categoria correta do slot informado.
            var belongsToSlot = request.Slot switch
            {
                "frame" => request.ItemKey.StartsWith(FramePrefix, StringComparison.Ordinal),
                "aura" => request.ItemKey.StartsWith(AuraPrefix, StringComparison.Ordinal),
                "background" => request.ItemKey.StartsWith(BackgroundPrefix, StringComparison.Ordinal),
                _ => false,
            };

            if (!belongsToSlot)
            {
                throw new ConflictException(
                    "COSMETIC_CATEGORY_MISMATCH",
                    "Este item não pertence a esta categoria.");
            }

            // RN-002: só pode equipar cosméticos que o usuário efetivamente possui.
            var item = await inventoryRepository.GetByUserIdAndItemKeyAsync(
                userId, request.ItemKey, cancellationToken);

            if (item is null || item.Quantity <= 0)
            {
                throw new ConflictException(
                    "COSMETIC_NOT_OWNED",
                    "Você não possui este item.");
            }
        }

        var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("HunterProgression", userId);

        switch (request.Slot)
        {
            case "frame":
                progression.EquipFrame(request.ItemKey, utcNow);
                break;
            case "aura":
                progression.EquipAura(request.ItemKey, utcNow);
                break;
            case "background":
                progression.EquipBackground(request.ItemKey, utcNow);
                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
