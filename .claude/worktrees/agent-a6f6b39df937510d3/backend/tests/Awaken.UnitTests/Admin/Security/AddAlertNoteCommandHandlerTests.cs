using Awaken.Application.Admin.Security.Commands.AddAlertNote;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Admin.Security;

/// <summary>US-219: adicionar nota de triagem deve persistir e gerar auditoria (RN-004).</summary>
public class AddAlertNoteCommandHandlerTests
{
    private readonly Mock<ISecurityAlertRepository> _securityAlertRepository = new();
    private readonly Mock<ICurrentAdminService> _currentAdminService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _adminId = Guid.NewGuid();

    public AddAlertNoteCommandHandlerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
        _currentAdminService.Setup(s => s.AdminUserId).Returns(_adminId);
    }

    private AddAlertNoteCommandHandler CreateHandler() => new(
        _securityAlertRepository.Object,
        _currentAdminService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _auditLogService.Object);

    private static SecurityAlert CreateAlert() =>
        SecurityAlert.Create("rbac_denied", "high", "prod", UtcNow, origin: "admin_panel", maskedIp: "10.0.0.x");

    [Fact]
    public async Task Handle_AddsNote_ToAlert()
    {
        var alert = CreateAlert();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        var result = await CreateHandler().Handle(new AddAlertNoteCommand(alert.Id, "Validado com o time de plataforma."), CancellationToken.None);

        result.Should().Be(Unit.Value);
        alert.Note.Should().Be("Validado com o time de plataforma.");
    }

    [Fact]
    public async Task Handle_GeneratesAuditLogEntry()
    {
        var alert = CreateAlert();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        await CreateHandler().Handle(new AddAlertNoteCommand(alert.Id, "Nota de teste."), CancellationToken.None);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminSecurityAlertNoteAdded,
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
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityAlert?)null);

        var act = async () => await CreateHandler().Handle(new AddAlertNoteCommand(alertId, "Nota"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
