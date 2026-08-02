using Awaken.Application.Admin.Bugs.Commands.UpdateOperationalBug;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Bugs;
using Awaken.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Admin.Bugs;

public class UpdateOperationalBugCommandHandlerTests
{
    private readonly Mock<IOperationalBugRepository> _operationalBugRepository = new();
    private readonly Mock<IOperationalBugEventRepository> _operationalBugEventRepository = new();
    private readonly Mock<ICurrentAdminService> _currentAdminService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private static readonly DateTime UtcNow = new(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _adminId = Guid.NewGuid();

    public UpdateOperationalBugCommandHandlerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
        _currentAdminService.Setup(s => s.AdminUserId).Returns(_adminId);
    }

    private UpdateOperationalBugCommandHandler CreateHandler() => new(
        _operationalBugRepository.Object,
        _operationalBugEventRepository.Object,
        _currentAdminService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _auditLogService.Object);

    private static OperationalBug CreateBug() => OperationalBug.Create(
        "Crash no login",
        "high",
        "auth-service",
        "prod",
        "internal",
        Guid.NewGuid(),
        UtcNow,
        UtcNow);

    [Fact]
    public async Task HandleStatusChangeCreatesEventAndAdminBugUpdatedAudit()
    {
        var bug = CreateBug();
        _operationalBugRepository.Setup(r => r.GetByIdAsync(bug.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bug);

        var command = new UpdateOperationalBugCommand(bug.Id, "in_progress", null, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        bug.Status.Should().Be("in_progress");

        _operationalBugEventRepository.Verify(r => r.AddAsync(
            It.Is<OperationalBugEvent>(e =>
                e.BugId == bug.Id &&
                e.EventType == "status_change" &&
                e.OldValue == "open" &&
                e.NewValue == "in_progress" &&
                e.AdminId == _adminId),
            It.IsAny<CancellationToken>()), Times.Once);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminBugUpdated,
            _adminId,
            AuditActorType.Admin,
            AuditResourceTypes.OperationalBug,
            bug.Id,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleStatusChangeToClosedUsesAdminBugClosedAudit()
    {
        var bug = CreateBug();
        _operationalBugRepository.Setup(r => r.GetByIdAsync(bug.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bug);

        var command = new UpdateOperationalBugCommand(bug.Id, "closed", null, null);
        await CreateHandler().Handle(command, CancellationToken.None);

        bug.Status.Should().Be("closed");

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminBugClosed,
            _adminId,
            AuditActorType.Admin,
            AuditResourceTypes.OperationalBug,
            bug.Id,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAssigneeChangeCreatesAssignedEvent()
    {
        var bug = CreateBug();
        var newAdminId = Guid.NewGuid();
        _operationalBugRepository.Setup(r => r.GetByIdAsync(bug.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bug);

        var command = new UpdateOperationalBugCommand(bug.Id, null, newAdminId, null);
        await CreateHandler().Handle(command, CancellationToken.None);

        bug.AssignedAdminId.Should().Be(newAdminId);

        _operationalBugEventRepository.Verify(r => r.AddAsync(
            It.Is<OperationalBugEvent>(e =>
                e.BugId == bug.Id &&
                e.EventType == "assigned" &&
                e.NewValue == newAdminId.ToString() &&
                e.AdminId == _adminId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCommentOnlyCreatesCommentEvent()
    {
        var bug = CreateBug();
        _operationalBugRepository.Setup(r => r.GetByIdAsync(bug.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bug);

        var command = new UpdateOperationalBugCommand(bug.Id, null, null, "Investigando causa raiz.");
        await CreateHandler().Handle(command, CancellationToken.None);

        _operationalBugEventRepository.Verify(r => r.AddAsync(
            It.Is<OperationalBugEvent>(e =>
                e.BugId == bug.Id &&
                e.EventType == "comment" &&
                e.Comment == "Investigando causa raiz." &&
                e.AdminId == _adminId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleThrowsNotFoundWhenBugMissing()
    {
        var bugId = Guid.NewGuid();
        _operationalBugRepository.Setup(r => r.GetByIdAsync(bugId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OperationalBug?)null);

        var command = new UpdateOperationalBugCommand(bugId, "open", null, null);
        var act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
