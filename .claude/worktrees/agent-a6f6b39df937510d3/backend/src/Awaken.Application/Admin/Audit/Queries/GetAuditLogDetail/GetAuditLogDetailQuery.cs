using Awaken.Contracts.Admin.Audit;
using MediatR;

namespace Awaken.Application.Admin.Audit.Queries.GetAuditLogDetail;

/// <summary>US-166: consulta de detalhe de uma entrada específica do log de auditoria.</summary>
public record GetAuditLogDetailQuery(Guid LogId) : IRequest<AuditLogDetailResponse>;
