using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Bugs;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Security.Commands.LinkAlertToBug;

/// <summary>
/// US-219: vincula um alerta de segurança a um bug/incidente operacional já existente.
/// Valida que o bug existe antes de gravar o vínculo. Gera auditoria (RN-004).
/// </summary>
public class LinkAlertToBugCommandHandler(
    ISecurityAlertRepository securityAlertRepository,
    IOperationalBugRepository operationalBugRepository,
    ICurrentAdminService currentAdminService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService) : IRequestHandler<LinkAlertToBugCommand, Unit>
{
    public async Task<Unit> Handle(LinkAlertToBugCommand request, CancellationToken cancellationToken)
    {
        var alert = await securityAlertRepository.GetByIdAsync(request.AlertId, cancellationToken)
            ?? throw new NotFoundException(nameof(SecurityAlert), request.AlertId);

        _ = await operationalBugRepository.GetByIdAsync(request.BugId, cancellationToken)
            ?? throw new NotFoundException(nameof(OperationalBug), request.BugId);

        var utcNow = dateTimeService.UtcNow;
        var adminId = currentAdminService.AdminUserId;

        alert.LinkToBug(request.BugId, adminId, utcNow);

        await auditLogService.RecordAsync(
            AuditActions.AdminSecurityAlertLinkedToBug,
            adminId,
            AuditActorType.Admin,
            AuditResourceTypes.SecurityAlert,
            alert.Id,
            metadataSafe: $"{{\"alertType\":\"{alert.AlertType}\",\"bugId\":\"{request.BugId}\"}}",
            cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
