using Awaken.Contracts.Admin.Security;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Security.Queries.GetSecurityAlerts;

/// <summary>
/// US-165: handler de listagem/filtro de alertas de segurança para o admin site.
/// </summary>
public class GetSecurityAlertsQueryHandler(ISecurityAlertRepository securityAlertRepository)
    : IRequestHandler<GetSecurityAlertsQuery, SecurityAlertListResponse>
{
    public async Task<SecurityAlertListResponse> Handle(
        GetSecurityAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await securityAlertRepository.GetPagedAsync(
            request.AlertType,
            request.Severity,
            request.Status,
            request.Environment,
            request.Page,
            request.PageSize,
            cancellationToken);

        var projected = items
            .Select(a => new SecurityAlertSummaryResponse(
                a.Id,
                a.AlertType,
                a.Severity,
                a.Status,
                a.Origin,
                a.Environment,
                a.CreatedAtUtc,
                a.Classification))
            .ToList();

        return new SecurityAlertListResponse(projected, total, request.Page, request.PageSize);
    }
}
