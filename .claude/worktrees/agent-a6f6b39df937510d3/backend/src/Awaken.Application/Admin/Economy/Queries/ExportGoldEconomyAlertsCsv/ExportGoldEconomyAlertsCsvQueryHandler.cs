using System.Text;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Economy.Queries.ExportGoldEconomyAlertsCsv;

/// <summary>
/// US-228: monta o CSV de divergências da economia Gold a partir do mesmo SecurityAlert
/// genérico (US-165/US-219), filtrando apenas os AlertType emitidos pela reconciliação
/// (GoldEconomyAlertTypes). RN-006: nenhuma coluna sensível de pagamento/provider — apenas
/// tipo, severidade, status, usuário afetado (Id), ambiente e timestamps.
/// </summary>
public class ExportGoldEconomyAlertsCsvQueryHandler(
    ISecurityAlertRepository securityAlertRepository,
    ICurrentAdminService currentAdminService,
    IAuditLogService auditLogService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ExportGoldEconomyAlertsCsvQuery, byte[]>
{
    private const int MaxRows = 10_000;

    private static readonly HashSet<string> GoldEconomyTypes = new(StringComparer.Ordinal)
    {
        GoldEconomyAlertTypes.BalanceMismatch,
        GoldEconomyAlertTypes.NegativeBalance,
        GoldEconomyAlertTypes.LedgerMissing,
        GoldEconomyAlertTypes.OrderGrantedWithoutDebit,
        GoldEconomyAlertTypes.CreditWithoutValidation,
        GoldEconomyAlertTypes.ItemWithoutOrigin,
        GoldEconomyAlertTypes.DuplicatePurchase,
        GoldEconomyAlertTypes.AbnormalVolume,
        GoldEconomyAlertTypes.ExcessiveFailures,
    };

    public async Task<byte[]> Handle(ExportGoldEconomyAlertsCsvQuery request, CancellationToken ct)
    {
        var (items, _) = await securityAlertRepository.GetPagedAsync(
            null, request.Severity, request.Status, null, 1, MaxRows, ct);

        var goldAlerts = items.Where(a => GoldEconomyTypes.Contains(a.AlertType)).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("AlertType,Severity,Status,AffectedUserId,Environment,CreatedAtUtc,Classification");

        foreach (var alert in goldAlerts)
        {
            sb.AppendLine(string.Join(",",
                alert.AlertType,
                alert.Severity,
                alert.Status,
                alert.AffectedUserId?.ToString() ?? "",
                alert.Environment,
                alert.CreatedAtUtc.ToString("O"),
                alert.Classification ?? ""));
        }

        var adminId = currentAdminService.AdminUserId;

        await auditLogService.RecordAsync(
            AuditActions.AdminReportsExported,
            adminId,
            AuditActorType.Admin,
            AuditResourceTypes.SecurityAlert,
            null,
            null,
            ct);

        await unitOfWork.SaveChangesAsync(ct);

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
