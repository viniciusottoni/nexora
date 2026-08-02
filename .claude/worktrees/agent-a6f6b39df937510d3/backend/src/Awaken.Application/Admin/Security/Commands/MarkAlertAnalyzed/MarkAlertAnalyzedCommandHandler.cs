using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Security.Commands.MarkAlertAnalyzed;

/// <summary>
/// US-165 RN-004: marcar um alerta de segurança como analisado e auditar a ação,
/// especialmente para alertas com Severity = "critical".
/// </summary>
public class MarkAlertAnalyzedCommandHandler(
    ISecurityAlertRepository securityAlertRepository,
    ICurrentAdminService currentAdminService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService) : IRequestHandler<MarkAlertAnalyzedCommand, Unit>
{
    private const int MaxNoteLength = 500;

    public async Task<Unit> Handle(MarkAlertAnalyzedCommand request, CancellationToken cancellationToken)
    {
        var alert = await securityAlertRepository.GetByIdAsync(request.AlertId, cancellationToken)
            ?? throw new NotFoundException(nameof(SecurityAlert), request.AlertId);

        var utcNow = dateTimeService.UtcNow;
        var adminId = currentAdminService.AdminUserId;

        alert.MarkAnalyzed(adminId, utcNow);

        var safeNote = TruncateSafe(request.Note);
        var metadataSafe = safeNote is null
            ? $"{{\"alertType\":\"{alert.AlertType}\",\"severity\":\"{alert.Severity}\"}}"
            : $"{{\"alertType\":\"{alert.AlertType}\",\"severity\":\"{alert.Severity}\",\"note\":\"{safeNote}\"}}";

        await auditLogService.RecordAsync(
            AuditActions.AdminSecurityAlertAnalyzed,
            adminId,
            AuditActorType.Admin,
            AuditResourceTypes.SecurityAlert,
            alert.Id,
            metadataSafe: metadataSafe,
            cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private static string? TruncateSafe(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return null;

        var trimmed = note.Trim();
        var truncated = trimmed.Length > MaxNoteLength ? trimmed[..MaxNoteLength] : trimmed;

        // Escapa aspas para manter o JSON simples do metadataSafe válido.
        return truncated.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
