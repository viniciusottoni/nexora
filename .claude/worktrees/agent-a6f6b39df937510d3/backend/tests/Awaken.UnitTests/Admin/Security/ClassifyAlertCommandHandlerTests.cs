using Awaken.Application.Admin.Security.Commands.ClassifyAlert;
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
/// US-219 RN-005: classificar um alerta como falso positivo NUNCA apaga o alerta — apenas
/// altera sua classificação. RN-004: toda ação de triagem gera auditoria.
/// </summary>
public class ClassifyAlertCommandHandlerTests
{
    private readonly Mock<ISecurityAlertRepository> _securityAlertRepository = new();
    private readonly Mock<ICurrentAdminService> _currentAdminService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _adminId = Guid.NewGuid();

    public ClassifyAlertCommandHandlerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
        _currentAdminService.Setup(s => s.AdminUserId).Returns(_adminId);
    }

    private ClassifyAlertCommandHandler CreateHandler() => new(
        _securityAlertRepository.Object,
        _currentAdminService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _auditLogService.Object);

    private static SecurityAlert CreateAlert(string severity = "critical", string alertType = "brute_force") =>
        SecurityAlert.Create(alertType, severity, "prod", UtcNow, origin: "login", maskedIp: "10.0.0.x");

    [Fact]
    public async Task Handle_ClassifyAsFalsePositive_DoesNotDeleteAlert()
    {
        // RN-005: o registro deve continuar acessível e não marcado como excluído.
        var alert = CreateAlert();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        var command = new ClassifyAlertCommand(alert.Id, "false_positive", "Confirmado como teste de QA.");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        alert.IsDeleted.Should().BeFalse("RN-005: falso positivo não apaga o alerta");
        alert.Classification.Should().Be("false_positive");
    }

    [Fact]
    public async Task Handle_ClassifyAsFalsePositive_SetsClassificationAndAnalyzedFields()
    {
        var alert = CreateAlert();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        await CreateHandler().Handle(new ClassifyAlertCommand(alert.Id, "false_positive", null), CancellationToken.None);

        alert.Classification.Should().Be("false_positive");
        alert.AnalyzedByAdminId.Should().Be(_adminId);
        alert.AnalyzedAtUtc.Should().Be(UtcNow);
    }

    [Fact]
    public async Task Handle_ClassifyCriticalAlert_GeneratesAuditLogEntry()
    {
        var alert = CreateAlert(severity: "critical");
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        await CreateHandler().Handle(new ClassifyAlertCommand(alert.Id, "false_positive", null), CancellationToken.None);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminSecurityAlertClassified,
            _adminId,
            AuditActorType.Admin,
            AuditResourceTypes.SecurityAlert,
            alert.Id,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNote_PersistsNoteOnAlert()
    {
        var alert = CreateAlert();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        await CreateHandler().Handle(
            new ClassifyAlertCommand(alert.Id, "false_positive", "Teste de QA, sem impacto real."),
            CancellationToken.None);

        alert.Note.Should().Be("Teste de QA, sem impacto real.");
    }

    [Fact]
    public async Task Handle_AlertNotFound_ThrowsNotFoundException()
    {
        var alertId = Guid.NewGuid();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityAlert?)null);

        var act = async () => await CreateHandler().Handle(
            new ClassifyAlertCommand(alertId, "false_positive", null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PersistsChanges_ViaUnitOfWork()
    {
        var alert = CreateAlert();
        _securityAlertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        await CreateHandler().Handle(new ClassifyAlertCommand(alert.Id, "confirmed", null), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
