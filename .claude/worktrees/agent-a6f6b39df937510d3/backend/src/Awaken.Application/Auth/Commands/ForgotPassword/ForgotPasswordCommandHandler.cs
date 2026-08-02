using System.Security.Cryptography;
using System.Text;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetRepository passwordResetRepository,
    IEmailService emailService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<ForgotPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is not null)
        {
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var tokenHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            var resetRequest = PasswordResetRequest.Create(
                user.Id,
                tokenHash,
                utcNow,
                utcNow.AddHours(1));

            await passwordResetRepository.AddAsync(resetRequest, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await emailService.SendPasswordResetAsync(user.Email, rawToken, cancellationToken);
        }

        return Unit.Value;
    }
}
