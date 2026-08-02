using System.Text;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Users.Queries.ExportAdminUsersCsv;

/// <summary>
/// US-167: handler de exportação CSV de usuários para o site admin.
/// RN-002: colunas exportadas não incluem senha, token ou dados físicos (ADR-015).
/// Registra auditoria do export após gerar o arquivo.
/// </summary>
public class ExportAdminUsersCsvQueryHandler(
    IAdminUserQueryRepository repo,
    ICurrentAdminService currentAdminService,
    IAuditLogService auditLogService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ExportAdminUsersCsvQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportAdminUsersCsvQuery request, CancellationToken ct)
    {
        var (items, _) = await repo.GetPagedAsync(
            request.Search,
            request.Plan,
            request.Status,
            page: 1,
            pageSize: 10_000,
            ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,Email,DisplayName,Plan,SubscriptionStatus,IsEmailVerified,IsOnboardingComplete,LastLoginAtUtc,CreatedAtUtc");

        foreach (var r in items)
        {
            sb.AppendLine(string.Join(',',
                r.Id,
                EscapeCsv(r.Email),
                EscapeCsv(r.DisplayName),
                EscapeCsv(r.Plan),
                EscapeCsv(r.SubscriptionStatus),
                r.IsEmailVerified,
                r.IsOnboardingComplete,
                r.LastLoginAtUtc?.ToString("o"),
                r.CreatedAtUtc.ToString("o")));
        }

        var adminId = currentAdminService.AdminUserId;

        await auditLogService.RecordAsync(
            AuditActions.AdminUsersExported,
            adminId,
            AuditActorType.Admin,
            AuditResourceTypes.AdminUser,
            null,
            null,
            ct);

        await unitOfWork.SaveChangesAsync(ct);

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string? value)
    {
        if (value is null) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
