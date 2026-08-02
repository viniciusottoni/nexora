using Awaken.Application.Admin.Auth.Commands.AdminMfaVerify;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Admin;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Admin.Auth;

public class AdminMfaVerifyCommandHandlerTests
{
    private readonly Mock<IAdminUserRepository> _adminUserRepository = new();
    private readonly Mock<ITotpService> _totpService = new();
    private readonly Mock<IAdminJwtService> _adminJwtService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<ILogger<AdminMfaVerifyCommandHandler>> _logger = new();

    private static readonly DateTime Now = new(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc);

    public AdminMfaVerifyCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(Now);
    }

    private AdminMfaVerifyCommandHandler CreateHandler() => new(
        _adminUserRepository.Object,
        _totpService.Object,
        _adminJwtService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _auditLogService.Object,
        _logger.Object);

    private AdminUser CreateAdminUserWithSecret(string secret = "secret-key")
    {
        var adminUser = AdminUser.Create("admin@awaken.app", "hashed-password", Now.AddDays(-1));
        adminUser.SetMfaSecret(secret, Now.AddDays(-1));
        return adminUser;
    }

    [Fact]
    public async Task HandleReturnsTokenAndEnablesMfaOnFirstValidCode()
    {
        var adminUser = CreateAdminUserWithSecret();

        _adminUserRepository.Setup(r => r.GetByIdAsync(adminUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _totpService.Setup(t => t.VerifyCode("secret-key", "123456")).Returns(true);
        _adminJwtService.Setup(j => j.GenerateToken(adminUser)).Returns("access-token");

        var command = new AdminMfaVerifyCommand(adminUser.Id, "123456");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        adminUser.MfaEnabled.Should().BeTrue();

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleThrowsUnauthorizedExceptionWhenCodeInvalid()
    {
        var adminUser = CreateAdminUserWithSecret();

        _adminUserRepository.Setup(r => r.GetByIdAsync(adminUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _totpService.Setup(t => t.VerifyCode("secret-key", "000000")).Returns(false);

        var command = new AdminMfaVerifyCommand(adminUser.Id, "000000");

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Code.Should().Be("INVALID_MFA_CODE");

        _adminJwtService.Verify(j => j.GenerateToken(It.IsAny<AdminUser>()), Times.Never);
    }

    [Fact]
    public async Task HandleLocksAccountAfterFiveInvalidCodes()
    {
        var adminUser = CreateAdminUserWithSecret();

        _adminUserRepository.Setup(r => r.GetByIdAsync(adminUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _totpService.Setup(t => t.VerifyCode("secret-key", "000000")).Returns(false);

        var command = new AdminMfaVerifyCommand(adminUser.Id, "000000");

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
    public async Task HandleThrowsUnauthorizedExceptionWhenAccountLocked()
    {
        var adminUser = CreateAdminUserWithSecret();
        adminUser.Lock(TimeSpan.FromMinutes(15), Now);

        _adminUserRepository.Setup(r => r.GetByIdAsync(adminUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);

        var command = new AdminMfaVerifyCommand(adminUser.Id, "123456");

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Code.Should().Be("INVALID_MFA_CODE");

        _totpService.Verify(t => t.VerifyCode(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
