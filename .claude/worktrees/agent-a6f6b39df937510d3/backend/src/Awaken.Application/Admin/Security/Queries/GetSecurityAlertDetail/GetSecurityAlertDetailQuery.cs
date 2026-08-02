using Awaken.Contracts.Admin.Security;
using MediatR;

namespace Awaken.Application.Admin.Security.Queries.GetSecurityAlertDetail;

/// <summary>US-165: consulta de detalhe de um alerta de segurança específico.</summary>
public record GetSecurityAlertDetailQuery(Guid AlertId) : IRequest<SecurityAlertDetailResponse>;
