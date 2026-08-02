using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RefreshTokenEntity = Awaken.Domain.Entities.Auth.RefreshToken;

namespace Awaken.Application.Auth.Commands.Login;

public class LoginUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IRefreshTokenRepository refreshTokenRepository,
    ISubscriptionRepository subscriptionRepository,
    IConfiguration configuration,
    ILoginAttemptTracker loginAttemptTracker,
    ILogger<LoginUserCommandHandler> logger) : IRequestHandler<LoginUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        if (await loginAttemptTracker.IsLockedOutAsync(request.Email, cancellationToken))
            throw new TooManyRequestsException("TOO_MANY_REQUESTS", "Muitas tentativas de login. Tente novamente mais tarde.");

        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user?.PasswordHash is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await loginAttemptTracker.RecordFailureAsync(request.Email, cancellationToken);
            logger.LogWarning("Failed login attempt for email hash {EmailHash}", request.Email.GetHashCode());
            throw new UnauthorizedException("INVALID_CREDENTIALS", "E-mail ou senha inválidos.");
        }

        await loginAttemptTracker.ClearAsync(request.Email, cancellationToken);

        var utcNow = dateTimeService.UtcNow;
        user.RecordLogin(utcNow);

        var roles = user.Role is not null ? new[] { user.Role } : Array.Empty<string>();
        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, roles);
        var rawRefreshToken = jwtService.GenerateRefreshToken();
        var tokenHash = jwtService.HashRefreshToken(rawRefreshToken);
        var expiryMinutes = int.Parse(configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");
        var refreshExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? "30");

        await refreshTokenRepository.AddAsync(
            RefreshTokenEntity.Create(user.Id, tokenHash, utcNow.AddDays(refreshExpiryDays)),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var subscription = await subscriptionRepository.GetByUserIdAsync(user.Id, cancellationToken);
        var accessStatus = subscription?.Plan is "monthly" or "annual"
            ? subscription.ExpiresAt > utcNow ? "subscription_active" : "subscription_expired"
            : user.ComputeAccessStatus(utcNow);

        return new AuthResponse(
            accessToken,
            rawRefreshToken,
            utcNow.AddMinutes(expiryMinutes),
            new UserDto(
                user.Id,
                user.Email,
                user.DisplayName,
                user.AvatarUrl,
                user.PreferredLanguage,
                user.IsOnboardingComplete,
                accessStatus));
    }
}
