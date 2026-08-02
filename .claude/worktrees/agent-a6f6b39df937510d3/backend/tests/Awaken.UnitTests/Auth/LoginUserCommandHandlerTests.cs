using Awaken.Application.Auth.Commands.Login;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Auth;

public class LoginUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<ILoginAttemptTracker> _loginAttemptTracker = new();
    private readonly Mock<ILogger<LoginUserCommandHandler>> _logger = new();
    private readonly IConfiguration _configuration;

    public LoginUserCommandHandlerTests()
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
        _loginAttemptTracker
            .Setup(t => t.IsLockedOutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private LoginUserCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _passwordHasher.Object,
        _jwtService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _refreshTokenRepository.Object,
        _subscriptionRepository.Object,
        _configuration,
        _loginAttemptTracker.Object,
        _logger.Object);

    [Fact]
    public async Task HandleReturnsAuthResponseWhenCredentialsAreValid()
    {
        var user = User.Create("hunter@awaken.app", "hashed-password", "Hunter", "pt-BR");

        _userRepository.Setup(r => r.GetByEmailAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("Str0ngPass!", "hashed-password")).Returns(true);
        _jwtService.Setup(j => j.GenerateAccessToken(It.IsAny<Guid>(), "hunter@awaken.app", It.IsAny<string[]>()))
            .Returns("access-token");
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _jwtService.Setup(j => j.HashRefreshToken("refresh-token")).Returns("hashed-token");
        var now = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc);
        _dateTimeService.Setup(d => d.UtcNow).Returns(now);

        var command = new LoginUserCommand("hunter@awaken.app", "Str0ngPass!");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ExpiresAtUtc.Should().Be(now.AddMinutes(15));
        result.User.Email.Should().Be("hunter@awaken.app");
        result.User.DisplayName.Should().Be("Hunter");
        result.User.AccessStatus.Should().Be("no_trial");
        user.LastLoginAtUtc.Should().Be(now);
        user.UpdatedAtUtc.Should().Be(now);

        _refreshTokenRepository.Verify(
            r => r.AddAsync(It.Is<RefreshToken>(rt =>
                rt.UserId == user.Id &&
                rt.TokenHash == "hashed-token" &&
                rt.ExpiresAtUtc == now.AddDays(30)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleClearsLockoutCounterOnSuccessfulLogin()
    {
        var user = User.Create("hunter@awaken.app", "hashed-password", "Hunter", "pt-BR");

        _userRepository.Setup(r => r.GetByEmailAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("Str0ngPass!", "hashed-password")).Returns(true);
        _jwtService.Setup(j => j.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns("access-token");
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _jwtService.Setup(j => j.HashRefreshToken("refresh-token")).Returns("hashed-token");
        _dateTimeService.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);

        var command = new LoginUserCommand("hunter@awaken.app", "Str0ngPass!");
        await CreateHandler().Handle(command, CancellationToken.None);

        _loginAttemptTracker.Verify(t => t.ClearAsync("hunter@awaken.app", It.IsAny<CancellationToken>()), Times.Once);
        _loginAttemptTracker.Verify(t => t.RecordFailureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleThrowsUnauthorizedExceptionWhenUserDoesNotExist()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("missing@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new LoginUserCommand("missing@awaken.app", "Str0ngPass!");

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Code.Should().Be("INVALID_CREDENTIALS");

        _loginAttemptTracker.Verify(t => t.RecordFailureAsync("missing@awaken.app", It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleThrowsUnauthorizedExceptionWhenPasswordIsWrong()
    {
        var user = User.Create("hunter@awaken.app", "hashed-password", "Hunter", "pt-BR");

        _userRepository.Setup(r => r.GetByEmailAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("WrongPass!", "hashed-password")).Returns(false);

        var command = new LoginUserCommand("hunter@awaken.app", "WrongPass!");

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Code.Should().Be("INVALID_CREDENTIALS");

        _loginAttemptTracker.Verify(t => t.RecordFailureAsync("hunter@awaken.app", It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleThrowsTooManyRequestsWhenAccountIsLockedOut()
    {
        _loginAttemptTracker
            .Setup(t => t.IsLockedOutAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new LoginUserCommand("hunter@awaken.app", "Str0ngPass!");

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<TooManyRequestsException>();
        exception.Which.Code.Should().Be("TOO_MANY_REQUESTS");
        exception.Which.Message.Should().NotContain("lockout");
        exception.Which.Message.Should().NotContain("hunter@awaken.app");

        _userRepository.Verify(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
