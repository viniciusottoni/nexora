using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using RefreshTokenEntity = Awaken.Domain.Entities.Auth.RefreshToken;

namespace Awaken.Application.Auth.Commands.GoogleSignIn;

public class GoogleSignInCommandHandler(
    IUserRepository userRepository,
    IGoogleTokenValidator googleTokenValidator,
    IJwtService jwtService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IRefreshTokenRepository refreshTokenRepository,
    ISubscriptionRepository subscriptionRepository,
    IConfiguration configuration) : IRequestHandler<GoogleSignInCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(GoogleSignInCommand request, CancellationToken cancellationToken)
    {
        var payload = await googleTokenValidator.ValidateAsync(request.ProviderCredential, cancellationToken);
        if (payload is null)
            throw new UnauthorizedException("GOOGLE_AUTH_FAILED", "Não foi possível entrar com Google.");

        var utcNow = dateTimeService.UtcNow;
        var user = await userRepository.GetByProviderAsync(AuthProvider.Google, payload.ProviderUserId, cancellationToken);

        if (user is null)
        {
            user = await userRepository.GetByEmailAsync(payload.Email, cancellationToken);

            if (user is not null)
            {
                user.LinkGoogleProvider(payload.ProviderUserId, utcNow);
            }
            else
            {
                user = User.CreateFromGoogle(
                    payload.Email,
                    payload.ProviderUserId,
                    payload.Name,
                    payload.Picture);
                await userRepository.AddAsync(user, cancellationToken);
            }
        }

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
