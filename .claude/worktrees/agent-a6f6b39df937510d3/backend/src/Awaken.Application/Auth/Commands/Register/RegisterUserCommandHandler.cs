using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using RefreshTokenEntity = Awaken.Domain.Entities.Auth.RefreshToken;

namespace Awaken.Application.Auth.Commands.Register;

public class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IRefreshTokenRepository refreshTokenRepository,
    IConfiguration configuration) : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new ConflictException("EMAIL_ALREADY_EXISTS", "Este e-mail já possui uma conta.");

        var utcNow = dateTimeService.UtcNow;
        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(
            request.Email,
            passwordHash,
            request.DisplayName,
            request.Language);

        await userRepository.AddAsync(user, cancellationToken);

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
                user.ComputeAccessStatus(utcNow)));
    }
}
