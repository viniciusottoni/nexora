using Awaken.Application.Auth.Commands.RefreshToken;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Awaken.UnitTests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IConfiguration _configuration;

    private readonly DateTime _utcNow = new(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);

    public RefreshTokenCommandHandlerTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "30",
            })
            .Build();

        _dateTimeService.Setup(d => d.UtcNow).Returns(_utcNow);
        _subscriptionRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
    }

    private RefreshTokenCommandHandler CreateHandler() => new(
        _refreshTokenRepository.Object,
        _userRepository.Object,
        _subscriptionRepository.Object,
        _jwtService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _configuration);

    [Fact]
    public async Task HandleReturnsNewAuthResponseOnValidRefreshToken()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");
        var storedToken = RefreshToken.Create(user.Id, "hashed-old-token", _utcNow.AddDays(30));

        _jwtService.Setup(j => j.HashRefreshToken("raw-old-token")).Returns("hashed-old-token");
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync("hashed-old-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwtService.Setup(j => j.GenerateAccessToken(user.Id, user.Email, It.IsAny<string[]>()))
            .Returns("new-access-token");
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("raw-new-token");
        _jwtService.Setup(j => j.HashRefreshToken("raw-new-token")).Returns("hashed-new-token");

        var command = new RefreshTokenCommand("raw-old-token");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("raw-new-token");
        storedToken.IsRevoked.Should().BeTrue();
        _refreshTokenRepository.Verify(
            r => r.AddAsync(It.Is<RefreshToken>(rt =>
                rt.UserId == user.Id && rt.TokenHash == "hashed-new-token"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleReturnsSubscriptionActiveWhenPaidPlanExistsAfterTrialExpired()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");
        user.StartTrial(_utcNow.AddDays(-1));
        var storedToken = RefreshToken.Create(user.Id, "hashed-old-token", _utcNow.AddDays(30));
        var subscription = Subscription.CreateFromPaidPlan(
            user.Id,
            "monthly",
            "pro_access",
            "rc_123",
            _utcNow.AddDays(30),
            _utcNow);

        _jwtService.Setup(j => j.HashRefreshToken("raw-old-token")).Returns("hashed-old-token");
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync("hashed-old-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository
            .Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _jwtService.Setup(j => j.GenerateAccessToken(user.Id, user.Email, It.IsAny<string[]>()))
            .Returns("new-access-token");
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("raw-new-token");
        _jwtService.Setup(j => j.HashRefreshToken("raw-new-token")).Returns("hashed-new-token");

        var result = await CreateHandler().Handle(
            new RefreshTokenCommand("raw-old-token"),
            CancellationToken.None);

        result.User.AccessStatus.Should().Be("subscription_active");
    }

    [Fact]
    public async Task HandleThrowsWhenTokenNotFound()
    {
        _jwtService.Setup(j => j.HashRefreshToken("unknown-token")).Returns("hashed-unknown");
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync("hashed-unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var act = () => CreateHandler().Handle(new RefreshTokenCommand("unknown-token"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }

    [Fact]
    public async Task HandleThrowsWhenTokenIsRevoked()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");
        var revokedToken = RefreshToken.Create(user.Id, "hashed-token", _utcNow.AddDays(30));
        revokedToken.Revoke(_utcNow);

        _jwtService.Setup(j => j.HashRefreshToken("raw-token")).Returns("hashed-token");
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync("hashed-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        var act = () => CreateHandler().Handle(new RefreshTokenCommand("raw-token"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }

    [Fact]
    public async Task HandleThrowsWhenTokenIsExpired()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");
        var expiredToken = RefreshToken.Create(user.Id, "hashed-token", _utcNow.AddDays(-1));

        _jwtService.Setup(j => j.HashRefreshToken("raw-token")).Returns("hashed-token");
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync("hashed-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var act = () => CreateHandler().Handle(new RefreshTokenCommand("raw-token"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }
}
