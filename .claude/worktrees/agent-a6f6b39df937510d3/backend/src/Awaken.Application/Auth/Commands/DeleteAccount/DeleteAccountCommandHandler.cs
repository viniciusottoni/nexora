using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Auth.Commands.DeleteAccount;

public class DeleteAccountCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService) : IRequestHandler<DeleteAccountCommand, Unit>
{
    public async Task<Unit> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        user.SoftDelete(userId, dateTimeService.UtcNow);
        userRepository.Update(user);

        await refreshTokenRepository.RevokeAllByUserIdAsync(userId, cancellationToken);

        await auditLogService.RecordAsync(
            "account_deleted",
            userId,
            AuditActorType.User,
            "User",
            userId,
            null,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
