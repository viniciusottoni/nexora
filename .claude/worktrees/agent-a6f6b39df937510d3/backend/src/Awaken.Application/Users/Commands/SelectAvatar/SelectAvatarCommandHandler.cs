using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Avatars;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Users.Commands.SelectAvatar;

public class SelectAvatarCommandHandler(
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUserRepository userRepository,
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<SelectAvatarCommand, Unit>
{
    public async Task<Unit> Handle(SelectAvatarCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;

        // RN-003/RN-004: so aceita chaves do catalogo interno - bloqueia
        // qualquer tentativa de "upload" via URL/valor arbitrario (CA-003).
        var avatar = AvatarCatalog.Find(request.AvatarKey)
            ?? throw new NotFoundException("Avatar", request.AvatarKey);

        if (avatar.RequiredItemKey is not null)
        {
            var item = await inventoryRepository.GetByUserIdAndItemKeyAsync(
                userId, avatar.RequiredItemKey, cancellationToken);

            // RN-005: avatares de pack so podem ser selecionados se o usuario
            // possuir o pack exigido.
            if (item is null || item.Quantity <= 0)
            {
                throw new ConflictException(
                    "AVATAR_LOCKED",
                    "Este avatar exige um pack que você ainda não possui.");
            }
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");

        user.SelectAvatar(avatar.AvatarKey, utcNow);
        userRepository.Update(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
