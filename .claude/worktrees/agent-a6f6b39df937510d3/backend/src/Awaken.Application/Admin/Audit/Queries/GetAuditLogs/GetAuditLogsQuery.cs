using Awaken.Contracts.Admin.Audit;
using MediatR;

namespace Awaken.Application.Admin.Audit.Queries.GetAuditLogs;

/// <summary>
/// US-166: consulta administrativa paginada do log de auditoria.
/// Todos os filtros são opcionais; ordenação por CreatedAtUtc desc (definida no repositório).
/// </summary>
public record GetAuditLogsQuery(
    string? ActorType,
    string? Action,
    string? ResourceType,
    DateTime? From,
    DateTime? To,
    int Page,
    int PageSize) : IRequest<AuditLogListResponse>;
