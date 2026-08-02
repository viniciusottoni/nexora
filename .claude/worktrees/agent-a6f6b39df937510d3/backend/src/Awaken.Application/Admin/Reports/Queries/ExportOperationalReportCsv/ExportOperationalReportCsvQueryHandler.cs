using System.Text;
using Awaken.Application.Admin.Reports.Queries.GetOperationalReport;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Reports.Queries.ExportOperationalReportCsv;

/// <summary>
/// US-170: handler de exportação CSV do relatório operacional.
/// Reutiliza GetOperationalReportQuery para buscar os dados e serializa em CSV.
/// Registra auditoria do export (ADR-015: sem dados sensíveis no log).
/// </summary>
public class ExportOperationalReportCsvQueryHandler(
    ISender sender,
    ICurrentAdminService currentAdminService,
    IAuditLogService auditLogService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ExportOperationalReportCsvQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportOperationalReportCsvQuery request, CancellationToken ct)
    {
        var report = await sender.Send(
            new GetOperationalReportQuery(request.From, request.To, request.Environment),
            ct);

        var sb = new StringBuilder();

        // Daily ops section
        sb.AppendLine("Section,Metric,Value");
        sb.AppendLine($"DailyOps,Period,{report.From} to {report.To}");
        sb.AppendLine($"DailyOps,Environment,{report.Environment}");
        sb.AppendLine($"DailyOps,TotalUsers,{report.DailyOps.TotalUsers}");
        sb.AppendLine($"DailyOps,DAU,{report.DailyOps.Dau}");
        sb.AppendLine($"DailyOps,OpenTickets,{report.DailyOps.OpenTickets}");
        sb.AppendLine($"DailyOps,NewTickets,{report.DailyOps.NewTickets}");
        sb.AppendLine($"DailyOps,OpenBugs,{report.DailyOps.OpenBugs}");
        sb.AppendLine($"DailyOps,NewBugs,{report.DailyOps.NewBugs}");
        sb.AppendLine($"DailyOps,OpenAlerts,{report.DailyOps.OpenAlerts}");

        // Support section
        sb.AppendLine($"Support,Total,{report.Support.Total}");
        sb.AppendLine($"Support,Open,{report.Support.Open}");
        sb.AppendLine($"Support,InProgress,{report.Support.InProgress}");
        sb.AppendLine($"Support,Resolved,{report.Support.Resolved}");
        sb.AppendLine($"Support,Closed,{report.Support.Closed}");
        sb.AppendLine($"Support,HighPriority,{report.Support.HighPriority}");

        // Technical section
        sb.AppendLine($"Technical,TotalBugs,{report.Technical.TotalBugs}");
        sb.AppendLine($"Technical,CriticalBugs,{report.Technical.CriticalBugs}");
        sb.AppendLine($"Technical,HighBugs,{report.Technical.HighBugs}");
        sb.AppendLine($"Technical,ResolvedThisPeriod,{report.Technical.ResolvedThisPeriod}");

        // Product section
        sb.AppendLine($"Product,TopEventName,{report.Product.TopEventName}");
        sb.AppendLine($"Product,TopEventCount,{report.Product.TopEventCount}");
        sb.AppendLine($"Product,DAU,{report.Product.Dau}");
        sb.AppendLine($"Product,MAU,{report.Product.Mau}");
        sb.AppendLine($"Product,DAU/MAU,{report.Product.DauMauRatio:F4}");

        var adminId = currentAdminService.AdminUserId;

        await auditLogService.RecordAsync(
            AuditActions.AdminReportsExported,
            adminId,
            AuditActorType.Admin,
            AuditResourceTypes.AdminUser,
            null,
            null,
            ct);

        await unitOfWork.SaveChangesAsync(ct);

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
