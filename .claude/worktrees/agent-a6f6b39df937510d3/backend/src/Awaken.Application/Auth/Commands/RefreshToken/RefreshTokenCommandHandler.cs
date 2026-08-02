using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using RefreshTokenEntity = Awaken.Domain.Entities.Auth.RefreshToken;

namespace Awaken.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    IJwtService jwtService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IConfiguration configuration) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtService.HashRefreshToken(request.RefreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        var utcNow = dateTimeService.UtcNow;

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAtUtc <= utcNow)
            throw new UnauthorizedException("SESSION_INVALID", "Sua sessão expirou. Entre novamente.");

        var user = await userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null)
            throw new UnauthorizedException("SESSION_INVALID", "Sua sessão expirou. Entre novamente.");

        storedToken.Revoke(utcNow);

        var roles = user.Role is not null ? new[] { user.Role } : Array.Empty<string>();
        var newAccessToken = jwtService.GenerateAccessToken(user.Id, user.Email, roles);
        var newRawRefreshToken = jwtService.GenerateRefreshToken();
        var newTokenHash = jwtService.HashRefreshToken(newRawRefreshToken);
        var refreshExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? "30");
        var accessExpiryMinutes = int.Parse(configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");

        await refreshTokenRepository.AddAsync(
            RefreshTokenEntity.Create(user.Id, newTokenHash, utcNow.AddDays(refreshExpiryDays)),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var subscription = await subscriptionRepository.GetByUserIdAsync(user.Id, cancellationToken);
        var accessStatus = subscription?.Plan is "monthly" or "annual"
            ? subscription.ExpiresAt > utcNow ? "subscription_active" : "subscription_expired"
            : user.ComputeAccessStatus(utcNow);

        return new AuthResponse(
            newAccessToken,
            newRawRefreshToken,
            utcNow.AddMinutes(accessExpiryMinutes),
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
