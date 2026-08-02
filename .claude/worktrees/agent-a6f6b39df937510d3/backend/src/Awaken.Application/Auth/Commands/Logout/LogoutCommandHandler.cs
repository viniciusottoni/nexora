using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Auth.Commands.Logout;

public class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork) : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await refreshTokenRepository.RevokeAllByUserIdAsync(currentUserService.UserId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
