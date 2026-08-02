using Awaken.Application.Admin.Security.Commands.MarkAlertAnalyzed;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Admin.Security;

/// <summary>
/// US-165 RN-004: marcar alerta como analisado deve atualizar o status e gerar auditoria,
/// especialmente relevante para alertas de severidade "critical".
/// </summary>
public class MarkAlertAnalyzedCommandHandlerTests
{
    private readonly Mock<ISecurityAlertRepository> _securityAlertRepository = new();
    private readonly Mock<ICurrentAdminService> _currentAdminService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private static readonly DateTime UtcNow = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _adminId = Guid.NewGuid();

    public MarkAlertAnalyzedCommandHandlerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
        _currentAdminService.Setup(s => s.AdminUserId).Returns(_adminId);
    }

    private MarkAlertAnalyzedCommandHandler CreateHandler() => new(
        _securityAlertRepository.Object,
        _currentAdminService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _auditLogService.Object);

    private static SecurityAlert CreateAlert(string severity = "critical") =>
        SecurityAlert.Create("brute_force", severity, "prod", UtcNow, origin: "login", maskedIp: "10.0.0.x");

    [Fact]
    public async Task Handle_MarksAlertAnalyzed_SetsAdminIdAndTimestamp()
    {
        var alert = CreateAlert();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        var command = new MarkAlertAnalyzedCommand(alert.Id, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        alert.Status.Should().Be("analyzed");
        alert.AnalyzedByAdminId.Should().Be(_adminId);
        alert.AnalyzedAtUtc.Should().Be(UtcNow);
    }

    [Fact]
    public async Task Handle_CreatesAuditLogEntry_WithAdminActorType()
    {
        var alert = CreateAlert();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        var command = new MarkAlertAnalyzedCommand(alert.Id, "Verificado, falso positivo.");
        await CreateHandler().Handle(command, CancellationToken.None);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminSecurityAlertAnalyzed,
            _adminId,
            AuditActorType.Admin,
            AuditResourceTypes.SecurityAlert,
            alert.Id,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PersistsChanges_ViaUnitOfWork()
    {
        var alert = CreateAlert();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        await CreateHandler().Handle(new MarkAlertAnalyzedCommand(alert.Id, null), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlertNotFound_ThrowsNotFoundException()
    {
        var alertId = Guid.NewGuid();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityAlert?)null);

        var command = new MarkAlertAnalyzedCommand(alertId, null);
        var act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NoteTooLong_IsTruncatedInMetadata()
    {
        var alert = CreateAlert();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        var longNote = new string('x', 1000);
        string? capturedMetadata = null;
        _auditLogService
            .Setup(a => a.RecordAsync(
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, AuditActorType, string, Guid?, string?, CancellationToken>(
                (_, _, _, _, _, meta, _) => capturedMetadata = meta)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(new MarkAlertAnalyzedCommand(alert.Id, longNote), CancellationToken.None);

        capturedMetadata.Should().NotBeNull();
        capturedMetadata!.Length.Should().BeLessThan(longNote.Length,
            "nota deve ser truncada para evitar payloads grandes em auditoria");
    }

    [Fact]
    public async Task Handle_DoesNotStoreRawUnmaskedIp_InAuditMetadata()
    {
        // RN-002: IP/identificadores devem ser minimizados/mascarados; a auditoria
        // nunca deve carregar um IP não mascarado.
        var alert = CreateAlert();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        string? capturedMetadata = null;
        _auditLogService
            .Setup(a => a.RecordAsync(
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, AuditActorType, string, Guid?, string?, CancellationToken>(
                (_, _, _, _, _, meta, _) => capturedMetadata = meta)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(new MarkAlertAnalyzedCommand(alert.Id, null), CancellationToken.None);

        capturedMetadata.Should().NotContain(alert.MaskedIp ?? string.Empty,
            "metadados de auditoria não devem conter o IP, mascarado ou não (ADR-015)");
    }
}
