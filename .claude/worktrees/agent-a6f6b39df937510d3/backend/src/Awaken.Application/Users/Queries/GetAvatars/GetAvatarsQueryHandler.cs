using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Users;
using Awaken.Domain.Entities.Avatars;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Users.Queries.GetAvatars;

public class GetAvatarsQueryHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IInventoryRepository inventoryRepository) : IRequestHandler<GetAvatarsQuery, IReadOnlyList<AvatarCatalogItemResponse>>
{
    public async Task<IReadOnlyList<AvatarCatalogItemResponse>> Handle(
        GetAvatarsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");

        // RN-001/RN-002: sem selecao manual, o avatar padrao efetivo e o do
        // Google (fica fora do catalogo - nenhuma linha aparece selecionada)
        // ou, na ausencia dele, o padrao do sistema para o sexo biologico do
        // onboarding (mesma regra de GetHunterProfileQueryHandler).
        string? selectedAvatarKey = user.SelectedAvatarKey;
        if (selectedAvatarKey is null && string.IsNullOrEmpty(user.AvatarUrl))
        {
            var onboardingProfile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            selectedAvatarKey = AvatarCatalog.GenderDefaultAvatarKey(onboardingProfile?.BiologicalSex);
        }

        var result = new List<AvatarCatalogItemResponse>(AvatarCatalog.Avatars.Count);

        foreach (var avatar in AvatarCatalog.Avatars)
        {
            var isUnlocked = avatar.RequiredItemKey is null;
            if (!isUnlocked)
            {
                var item = await inventoryRepository.GetByUserIdAndItemKeyAsync(
                    userId, avatar.RequiredItemKey!, cancellationToken);
                isUnlocked = item is not null && item.Quantity > 0;
            }

            result.Add(new AvatarCatalogItemResponse(
                AvatarKey: avatar.AvatarKey,
                IsUnlocked: isUnlocked,
                IsSelected: avatar.AvatarKey == selectedAvatarKey,
                RequiredItemKey: avatar.RequiredItemKey));
        }

        return result;
    }
}
