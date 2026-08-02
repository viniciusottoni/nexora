using Awaken.Application.Admin.Auth.Commands.AdminLogin;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Admin;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Admin.Auth;

public class AdminLoginCommandHandlerTests
{
    private readonly Mock<IAdminUserRepository> _adminUserRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<ILogger<AdminLoginCommandHandler>> _logger = new();

    private static readonly DateTime Now = new(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc);

    public AdminLoginCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(Now);
    }

    private AdminLoginCommandHandler CreateHandler() => new(
        _adminUserRepository.Object,
        _passwordHasher.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _auditLogService.Object,
        _logger.Object);

    [Fact]
    public async Task HandleReturnsRequiresMfaSetupWhenMfaNotEnabled()
    {
        var adminUser = AdminUser.Create("admin@awaken.app", "hashed-password", Now.AddDays(-1));

        _adminUserRepository.Setup(r => r.GetByEmailAsync("admin@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _passwordHasher.Setup(h => h.Verify("Str0ngPass!", "hashed-password")).Returns(true);

        var command = new AdminLoginCommand("admin@awaken.app", "Str0ngPass!");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AccessToken.Should().BeNull();
        result.RequiresMfaSetup.Should().BeTrue();
        result.RequiresMfaChallenge.Should().BeFalse();
        result.AdminUserId.Should().Be(adminUser.Id);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleReturnsRequiresMfaChallengeWhenMfaEnabled()
    {
        var adminUser = AdminUser.Create("admin@awaken.app", "hashed-password", Now.AddDays(-1));
        adminUser.SetMfaSecret("secret", Now.AddDays(-1));
        adminUser.EnableMfa(Now.AddDays(-1));

        _adminUserRepository.Setup(r => r.GetByEmailAsync("admin@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _passwordHasher.Setup(h => h.Verify("Str0ngPass!", "hashed-password")).Returns(true);

        var command = new AdminLoginCommand("admin@awaken.app", "Str0ngPass!");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AccessToken.Should().BeNull();
        result.RequiresMfaSetup.Should().BeFalse();
        result.RequiresMfaChallenge.Should().BeTrue();
        result.AdminUserId.Should().Be(adminUser.Id);
    }

    [Fact]
    public async Task HandleThrowsUnauthorizedExceptionWhenEmailUnknown()
    {
        _adminUserRepository.Setup(r => r.GetByEmailAsync("missing@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        var command = new AdminLoginCommand("missing@awaken.app", "Str0ngPass!");

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Code.Should().Be("INVALID_CREDENTIALS");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleThrowsUnauthorizedExceptionWithSameShapeWhenPasswordWrong()
    {
        var adminUser = AdminUser.Create("admin@awaken.app", "hashed-password", Now.AddDays(-1));

        _adminUserRepository.Setup(r => r.GetByEmailAsync("admin@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _passwordHasher.Setup(h => h.Verify("WrongPass!", "hashed-password")).Returns(false);

        var command = new AdminLoginCommand("admin@awaken.app", "WrongPass!");

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task HandleLocksAccountAfterFiveFailedAttempts()
    {
        var adminUser = AdminUser.Create("admin@awaken.app", "hashed-password", Now.AddDays(-1));

        _adminUserRepository.Setup(r => r.GetByEmailAsync("admin@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _passwordHasher.Setup(h => h.Verify("WrongPass!", "hashed-password")).Returns(false);

        var command = new AdminLoginCommand("admin@awaken.app", "WrongPass!");

        for (var i = 0; i < 4; i++)
        {
            var act = () => CreateHandler().Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        adminUser.IsLocked(Now).Should().BeFalse();

        var fifthAttempt = () => CreateHandler().Handle(command, CancellationToken.None);
        await fifthAttempt.Should().ThrowAsync<UnauthorizedException>();

        adminUser.IsLocked(Now).Should().BeTrue();
    }

    [Fact]
    public async Task HandleThrowsGenericFailureWhenAccountLocked()
    {
        var adminUser = AdminUser.Create("admin@awaken.app", "hashed-password", Now.AddDays(-1));
        adminUser.Lock(TimeSpan.FromMinutes(15), Now);

        _adminUserRepository.Setup(r => r.GetByEmailAsync("admin@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);

        var command = new AdminLoginCommand("admin@awaken.app", "Str0ngPass!");

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Code.Should().Be("INVALID_CREDENTIALS");

        _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
