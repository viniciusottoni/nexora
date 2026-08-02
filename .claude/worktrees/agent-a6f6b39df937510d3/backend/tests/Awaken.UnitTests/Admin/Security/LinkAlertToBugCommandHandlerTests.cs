using Awaken.Application.Admin.Security.Commands.LinkAlertToBug;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Bugs;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Admin.Security;

/// <summary>US-219: vincular alerta a bug/incidente operacional deve validar existência do bug e gerar auditoria (RN-004).</summary>
public class LinkAlertToBugCommandHandlerTests
{
    private readonly Mock<ISecurityAlertRepository> _securityAlertRepository = new();
    private readonly Mock<IOperationalBugRepository> _operationalBugRepository = new();
    private readonly Mock<ICurrentAdminService> _currentAdminService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _adminId = Guid.NewGuid();

    public LinkAlertToBugCommandHandlerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
        _currentAdminService.Setup(s => s.AdminUserId).Returns(_adminId);
    }

    private LinkAlertToBugCommandHandler CreateHandler() => new(
        _securityAlertRepository.Object,
        _operationalBugRepository.Object,
        _currentAdminService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _auditLogService.Object);

    private static SecurityAlert CreateAlert() =>
        SecurityAlert.Create("brute_force", "critical", "prod", UtcNow, origin: "login", maskedIp: "10.0.0.x");

    private static OperationalBug CreateBug() =>
        OperationalBug.Create("Pico de brute force", "high", "auth", "prod", "monitoring", Guid.NewGuid(), UtcNow, UtcNow);

    [Fact]
    public async Task Handle_LinksAlertToBug()
    {
        var alert = CreateAlert();
        var bug = CreateBug();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _operationalBugRepository.Setup(r => r.GetByIdAsync(bug.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bug);

        var result = await CreateHandler().Handle(new LinkAlertToBugCommand(alert.Id, bug.Id), CancellationToken.None);

        result.Should().Be(Unit.Value);
        alert.RelatedBugId.Should().Be(bug.Id);
    }

    [Fact]
    public async Task Handle_GeneratesAuditLogEntry()
    {
        var alert = CreateAlert();
        var bug = CreateBug();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _operationalBugRepository.Setup(r => r.GetByIdAsync(bug.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bug);

        await CreateHandler().Handle(new LinkAlertToBugCommand(alert.Id, bug.Id), CancellationToken.None);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminSecurityAlertLinkedToBug,
            _adminId,
            AuditActorType.Admin,
            AuditResourceTypes.SecurityAlert,
            alert.Id,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlertNotFound_ThrowsNotFoundException()
    {
        var alertId = Guid.NewGuid();
        var bug = CreateBug();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alertId, It.IsAny<CancellationToken>())).ReturnsAsync((SecurityAlert?)null);

        var act = async () => await CreateHandler().Handle(new LinkAlertToBugCommand(alertId, bug.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_BugNotFound_ThrowsNotFoundException()
    {
        var alert = CreateAlert();
        var bugId = Guid.NewGuid();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>())).ReturnsAsync(alert);
        _operationalBugRepository.Setup(r => r.GetByIdAsync(bugId, It.IsAny<CancellationToken>())).ReturnsAsync((OperationalBug?)null);

        var act = async () => await CreateHandler().Handle(new LinkAlertToBugCommand(alert.Id, bugId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
