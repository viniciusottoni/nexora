using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Legal.Commands.AcceptResponsibilityNotice;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Legal;

public class AcceptResponsibilityNoticeCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private AcceptResponsibilityNoticeCommandHandler CreateHandler() => new(
        _currentUser.Object,
        _userRepository.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _auditLogService.Object);

    [Fact]
    public async Task HandleRegistersResponsibilityNoticeAcceptanceForAuthenticatedUser()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hashed-password", "Hunter", "pt-BR");
        var now = new DateTime(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc);

        _currentUser.Setup(c => c.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _dateTimeService.Setup(d => d.UtcNow).Returns(now);

        var command = new AcceptResponsibilityNoticeCommand("1.0.0");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.HasAcceptedResponsibilityNotice.Should().BeTrue();
        result.ResponsibilityNoticeVersion.Should().Be("1.0.0");
        result.ResponsibilityNoticeAcceptedAt.Should().Be(now);
        user.HasAcceptedResponsibilityNotice.Should().BeTrue();
        user.UpdatedAtUtc.Should().Be(now);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleIsIdempotentWhenNoticeAlreadyAccepted()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hashed-password", "Hunter", "pt-BR");
        var firstAcceptance = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        user.AcceptResponsibilityNotice("1.0.0", firstAcceptance);

        var now = new DateTime(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc);

        _currentUser.Setup(c => c.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _dateTimeService.Setup(d => d.UtcNow).Returns(now);

        var command = new AcceptResponsibilityNoticeCommand("1.0.0");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.HasAcceptedResponsibilityNotice.Should().BeTrue();
        result.ResponsibilityNoticeAcceptedAt.Should().Be(now);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleThrowsNotFoundExceptionWhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();

        _currentUser.Setup(c => c.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new AcceptResponsibilityNoticeCommand("1.0.0");

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleRecordsResponsibilityNoticeAcceptedAuditLog()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hashed-password", "Hunter", "pt-BR");
        _currentUser.Setup(c => c.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _dateTimeService.Setup(d => d.UtcNow)
            .Returns(new DateTime(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc));

        await CreateHandler().Handle(new AcceptResponsibilityNoticeCommand("1.0.0"), CancellationToken.None);

        _auditLogService.Verify(
            a => a.RecordAsync(
                "responsibility_notice_accepted",
                userId,
                AuditActorType.User,
                "User",
                userId,
                It.Is<string?>(s => s != null && s.Contains("1.0.0")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
