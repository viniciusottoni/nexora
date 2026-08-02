using Awaken.Contracts.Admin.Security;
using MediatR;

namespace Awaken.Application.Admin.Security.Queries.GetSecurityAlerts;

/// <summary>
/// US-165: consulta administrativa paginada de alertas de segurança.
/// Todos os filtros são opcionais; ordenação por CreatedAtUtc desc (definida no repositório).
/// </summary>
public record GetSecurityAlertsQuery(
    string? AlertType,
    string? Severity,
    string? Status,
    string? Environment,
    int Page,
    int PageSize) : IRequest<SecurityAlertListResponse>;
