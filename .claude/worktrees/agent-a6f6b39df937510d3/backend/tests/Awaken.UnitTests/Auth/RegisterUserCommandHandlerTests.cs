using Awaken.Application.Auth.Commands.Register;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Awaken.UnitTests.Auth;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly IConfiguration _configuration;

    public RegisterUserCommandHandlerTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "30",
            })
            .Build();
    }

    private RegisterUserCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _passwordHasher.Object,
        _jwtService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _refreshTokenRepository.Object,
        _configuration);

    [Fact]
    public async Task HandleCreatesUserAndReturnsAuthResponseWhenEmailIsAvailable()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("Str0ngPass!")).Returns("hashed-password");
        _jwtService.Setup(j => j.GenerateAccessToken(It.IsAny<Guid>(), "hunter@awaken.app", It.IsAny<string[]>()))
            .Returns("access-token");
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _jwtService.Setup(j => j.HashRefreshToken("refresh-token")).Returns("hashed-token");
        var now = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc);
        _dateTimeService.Setup(d => d.UtcNow).Returns(now);

        var command = new RegisterUserCommand("Hunter@Awaken.App", "Str0ngPass!", "Hunter", "pt-BR");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ExpiresAtUtc.Should().Be(now.AddMinutes(15));
        result.User.Email.Should().Be("hunter@awaken.app");
        result.User.DisplayName.Should().Be("Hunter");
        result.User.PreferredLanguage.Should().Be("pt-BR");
        result.User.AccessStatus.Should().Be("no_trial");

        _userRepository.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email == "hunter@awaken.app" &&
            u.PasswordHash == "hashed-password" &&
            u.TrialEndsAt == null), It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task HandleThrowsConflictExceptionWhenEmailAlreadyExists()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RegisterUserCommand("hunter@awaken.app", "Str0ngPass!", "Hunter");

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("EMAIL_ALREADY_EXISTS");

        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
