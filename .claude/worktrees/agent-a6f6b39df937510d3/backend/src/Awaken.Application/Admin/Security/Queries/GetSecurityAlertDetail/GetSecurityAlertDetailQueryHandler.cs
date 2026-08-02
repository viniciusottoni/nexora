using Awaken.Application.Common.Exceptions;
using Awaken.Contracts.Admin.Security;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Security.Queries.GetSecurityAlertDetail;

/// <summary>US-165: handler de detalhe de alerta de segurança.</summary>
public class GetSecurityAlertDetailQueryHandler(ISecurityAlertRepository securityAlertRepository)
    : IRequestHandler<GetSecurityAlertDetailQuery, SecurityAlertDetailResponse>
{
    public async Task<SecurityAlertDetailResponse> Handle(
        GetSecurityAlertDetailQuery request,
        CancellationToken cancellationToken)
    {
        var alert = await securityAlertRepository.GetByIdAsync(request.AlertId, cancellationToken)
            ?? throw new NotFoundException(nameof(SecurityAlert), request.AlertId);

        return new SecurityAlertDetailResponse(
            alert.Id,
            alert.AlertType,
            alert.Severity,
            alert.Status,
            alert.Origin,
            alert.MaskedIp,
            alert.AffectedUserId,
            alert.Environment,
            alert.AnalyzedByAdminId,
            alert.AnalyzedAtUtc,
            alert.CreatedAtUtc,
            alert.Classification,
            alert.Note,
            alert.RelatedBugId);
    }
}
