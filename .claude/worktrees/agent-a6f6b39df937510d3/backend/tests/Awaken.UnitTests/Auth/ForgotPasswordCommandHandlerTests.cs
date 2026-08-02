using Awaken.Application.Auth.Commands.ForgotPassword;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordResetRepository> _passwordResetRepository = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly DateTime UtcNow = new(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);

    private ForgotPasswordCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _passwordResetRepository.Object,
        _emailService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    public ForgotPasswordCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
    }

    [Fact]
    public async Task HandleReturnsUnitRegardlessOfWhetherEmailExists()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(
            new ForgotPasswordCommand("nobody@awaken.app"),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task HandleDoesNotSendEmailWhenEmailDoesNotExist()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await CreateHandler().Handle(
            new ForgotPasswordCommand("nobody@awaken.app"),
            CancellationToken.None);

        _emailService.Verify(e => e.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleCreatesResetRequestAndSendsEmailWhenEmailExists()
    {
        var user = CreateUser("hunter@awaken.app");
        _userRepository.Setup(r => r.GetByEmailAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        PasswordResetRequest? capturedRequest = null;
        _passwordResetRepository
            .Setup(r => r.AddAsync(It.IsAny<PasswordResetRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PasswordResetRequest, CancellationToken>((req, _) => capturedRequest = req)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new ForgotPasswordCommand("hunter@awaken.app"),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserId.Should().Be(user.Id);
        capturedRequest.ExpiresAtUtc.Should().Be(UtcNow.AddHours(1));
        capturedRequest.RequestedAtUtc.Should().Be(UtcNow);
        capturedRequest.UsedAtUtc.Should().BeNull();
        capturedRequest.TokenHash.Should().NotBeNullOrWhiteSpace();

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(e => e.SendPasswordResetAsync(
            "hunter@awaken.app",
            It.IsNotNull<string>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleStoresHashedTokenNotRawToken()
    {
        var user = CreateUser("hunter@awaken.app");
        _userRepository.Setup(r => r.GetByEmailAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        string? capturedRawToken = null;
        PasswordResetRequest? capturedRequest = null;

        _passwordResetRepository
            .Setup(r => r.AddAsync(It.IsAny<PasswordResetRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PasswordResetRequest, CancellationToken>((req, _) => capturedRequest = req)
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, raw, _) => capturedRawToken = raw)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new ForgotPasswordCommand("hunter@awaken.app"),
            CancellationToken.None);

        capturedRawToken.Should().NotBeNullOrWhiteSpace();
        capturedRequest!.TokenHash.Should().NotBe(capturedRawToken);
    }

    [Fact]
    public async Task HandleSendsCryptographicallyStrongRawToken()
    {
        var user = CreateUser("hunter@awaken.app");
        _userRepository.Setup(r => r.GetByEmailAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        string? capturedRawToken = null;
        _passwordResetRepository
            .Setup(r => r.AddAsync(It.IsAny<PasswordResetRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, raw, _) => capturedRawToken = raw)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new ForgotPasswordCommand("hunter@awaken.app"),
            CancellationToken.None);

        capturedRawToken.Should().NotBeNullOrWhiteSpace();
        capturedRawToken!.Length.Should().BeGreaterThanOrEqualTo(64);
    }

    private static User CreateUser(string email) =>
        User.Create(email, "hashed-password", "Hunter");
}
