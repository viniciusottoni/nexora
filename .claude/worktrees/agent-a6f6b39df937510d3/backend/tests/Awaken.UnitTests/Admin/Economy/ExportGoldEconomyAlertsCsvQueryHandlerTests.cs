using System.Text;
using Awaken.Application.Admin.Economy.Queries.ExportGoldEconomyAlertsCsv;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Economy;

/// <summary>
/// US-228 RN-006 / seção 5 e 12: o relatório exportado de alertas de economia Gold nunca
/// deve conter dados sensíveis de pagamento/provider — apenas tipo, severidade, status,
/// usuário afetado (Id), ambiente e timestamps.
/// </summary>
public class ExportGoldEconomyAlertsCsvQueryHandlerTests
{
    private readonly Mock<ISecurityAlertRepository> _securityAlertRepository = new();
    private readonly Mock<ICurrentAdminService> _currentAdminService = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    public ExportGoldEconomyAlertsCsvQueryHandlerTests()
    {
        _currentAdminService.Setup(s => s.AdminUserId).Returns(AdminId);
    }

    private ExportGoldEconomyAlertsCsvQueryHandler CreateHandler() => new(
        _securityAlertRepository.Object,
        _currentAdminService.Object,
        _auditLogService.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Handle_ReturnsOnlyGoldEconomyAlertTypes_FilteringOutOthers()
    {
        var userId = Guid.NewGuid();
        var goldAlert = SecurityAlert.Create(GoldEconomyAlertTypes.BalanceMismatch, "high", "prod", UtcNow, origin: "gold_reconciliation", affectedUserId: userId);
        var securityAlert = SecurityAlert.Create("brute_force", "critical", "prod", UtcNow, origin: "login");

        _securityAlertRepository
            .Setup(r => r.GetPagedAsync(null, null, null, null, 1, 10_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<SecurityAlert>)new List<SecurityAlert> { goldAlert, securityAlert }, 2));

        var bytes = await CreateHandler().Handle(new ExportGoldEconomyAlertsCsvQuery(null, null), CancellationToken.None);
        var csv = Encoding.UTF8.GetString(bytes);

        csv.Should().Contain(GoldEconomyAlertTypes.BalanceMismatch);
        csv.Should().NotContain("brute_force");
    }

    [Fact]
    public async Task Handle_CsvDoesNotContainSensitivePaymentFields()
    {
        var alert = SecurityAlert.Create(GoldEconomyAlertTypes.OrderGrantedWithoutDebit, "high", "prod", UtcNow, origin: "gold_reconciliation", affectedUserId: Guid.NewGuid());

        _securityAlertRepository
            .Setup(r => r.GetPagedAsync(null, null, null, null, 1, 10_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<SecurityAlert>)new List<SecurityAlert> { alert }, 1));

        var bytes = await CreateHandler().Handle(new ExportGoldEconomyAlertsCsvQuery(null, null), CancellationToken.None);
        var csv = Encoding.UTF8.GetString(bytes);

        csv.Should().NotContainAny("receipt", "token", "card", "cvv", "transactionData", "paymentMethod", "balance", "Balance");
    }

    [Fact]
    public async Task Handle_HeaderContainsOnlyExpectedSafeColumns()
    {
        _securityAlertRepository
            .Setup(r => r.GetPagedAsync(null, null, null, null, 1, 10_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<SecurityAlert>)new List<SecurityAlert>(), 0));

        var bytes = await CreateHandler().Handle(new ExportGoldEconomyAlertsCsvQuery(null, null), CancellationToken.None);
        var csv = Encoding.UTF8.GetString(bytes);

        csv.Should().StartWith("AlertType,Severity,Status,AffectedUserId,Environment,CreatedAtUtc,Classification");
    }

    [Fact]
    public async Task Handle_FiltersBySeverityWhenProvided()
    {
        _securityAlertRepository
            .Setup(r => r.GetPagedAsync(null, "critical", null, null, 1, 10_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<SecurityAlert>)new List<SecurityAlert>(), 0));

        await CreateHandler().Handle(new ExportGoldEconomyAlertsCsvQuery("critical", null), CancellationToken.None);

        _securityAlertRepository.Verify(r => r.GetPagedAsync(null, "critical", null, null, 1, 10_000, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RecordsAuditLogForExport()
    {
        _securityAlertRepository
            .Setup(r => r.GetPagedAsync(null, null, null, null, 1, 10_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<SecurityAlert>)new List<SecurityAlert>(), 0));

        await CreateHandler().Handle(new ExportGoldEconomyAlertsCsvQuery(null, null), CancellationToken.None);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminReportsExported,
            AdminId,
            AuditActorType.Admin,
            AuditResourceTypes.SecurityAlert,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
