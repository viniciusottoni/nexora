using Awaken.Application.Auth.Commands.GoogleSignIn;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Awaken.UnitTests.Auth;

public class GoogleSignInCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IGoogleTokenValidator> _googleTokenValidator = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly IConfiguration _configuration;

    public GoogleSignInCommandHandlerTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "30",
            })
            .Build();
        _subscriptionRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
    }

    private GoogleSignInCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _googleTokenValidator.Object,
        _jwtService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _refreshTokenRepository.Object,
        _subscriptionRepository.Object,
        _configuration);

    private void SetupTokenGeneration(DateTime now)
    {
        _jwtService.Setup(j => j.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns("access-token");
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _jwtService.Setup(j => j.HashRefreshToken("refresh-token")).Returns("hashed-token");
        _dateTimeService.Setup(d => d.UtcNow).Returns(now);
    }

    [Fact]
    public async Task HandleCreatesNewUserWhenEmailDoesNotExist()
    {
        var now = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc);
        SetupTokenGeneration(now);

        _googleTokenValidator.Setup(v => v.ValidateAsync("valid-id-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload("google-sub-1", "hunter@awaken.app", true, "Hunter", "https://avatar.url"));

        _userRepository.Setup(r => r.GetByProviderAsync(AuthProvider.Google, "google-sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.GetByEmailAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new GoogleSignInCommand("google", "valid-id-token");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.User.Email.Should().Be("hunter@awaken.app");
        result.User.DisplayName.Should().Be("Hunter");
        result.AccessToken.Should().Be("access-token");
        result.ExpiresAtUtc.Should().Be(now.AddMinutes(15));
        result.User.AccessStatus.Should().Be("no_trial");

        _userRepository.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email == "hunter@awaken.app" &&
            u.Provider == AuthProvider.Google &&
            u.ProviderUserId == "google-sub-1" &&
            u.TrialEndsAt == null &&
            u.LastLoginAtUtc == now &&
            u.UpdatedAtUtc == now), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(
            r => r.AddAsync(It.Is<RefreshToken>(rt =>
                rt.UserId == result.User.Id &&
                rt.TokenHash == "hashed-token" &&
                rt.ExpiresAtUtc == now.AddDays(30)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAuthenticatesExistingGoogleUserWithoutCreatingNewOne()
    {
        var now = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc);
        SetupTokenGeneration(now);

        var existingUser = User.CreateFromGoogle("hunter@awaken.app", "google-sub-1", "Hunter");

        _googleTokenValidator.Setup(v => v.ValidateAsync("valid-id-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload("google-sub-1", "hunter@awaken.app", true, "Hunter", null));

        _userRepository.Setup(r => r.GetByProviderAsync(AuthProvider.Google, "google-sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var command = new GoogleSignInCommand("google", "valid-id-token");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.User.Email.Should().Be("hunter@awaken.app");
        existingUser.LastLoginAtUtc.Should().Be(now);
        existingUser.UpdatedAtUtc.Should().Be(now);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleLinksGoogleProviderWhenEmailAlreadyExistsAsLocalAccount()
    {
        var now = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc);
        SetupTokenGeneration(now);

        var existingLocalUser = User.Create("hunter@awaken.app", "hashed-password", "Hunter");

        _googleTokenValidator.Setup(v => v.ValidateAsync("valid-id-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokenPayload("google-sub-1", "hunter@awaken.app", true, "Hunter", null));

        _userRepository.Setup(r => r.GetByProviderAsync(AuthProvider.Google, "google-sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.GetByEmailAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLocalUser);

        var command = new GoogleSignInCommand("google", "valid-id-token");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.User.Email.Should().Be("hunter@awaken.app");
        existingLocalUser.Provider.Should().Be(AuthProvider.Google);
        existingLocalUser.ProviderUserId.Should().Be("google-sub-1");
        existingLocalUser.LastLoginAtUtc.Should().Be(now);
        existingLocalUser.UpdatedAtUtc.Should().Be(now);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleThrowsUnauthorizedExceptionWhenTokenIsInvalid()
    {
        _googleTokenValidator.Setup(v => v.ValidateAsync("invalid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoogleTokenPayload?)null);

        var command = new GoogleSignInCommand("google", "invalid-token");

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Code.Should().Be("GOOGLE_AUTH_FAILED");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
