using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Security.Commands.ClassifyAlert;

/// <summary>
/// US-219 RN-005: classifica um alerta de segurança (ex.: "false_positive").
/// O alerta NUNCA é removido/apagado — somente o campo Classification (e, opcionalmente,
/// a nota de triagem) é alterado. Toda execução gera auditoria (RN-004).
/// </summary>
public class ClassifyAlertCommandHandler(
    ISecurityAlertRepository securityAlertRepository,
    ICurrentAdminService currentAdminService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService) : IRequestHandler<ClassifyAlertCommand, Unit>
{
    private const int MaxNoteLength = 500;

    public async Task<Unit> Handle(ClassifyAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = await securityAlertRepository.GetByIdAsync(request.AlertId, cancellationToken)
            ?? throw new NotFoundException(nameof(SecurityAlert), request.AlertId);

        var utcNow = dateTimeService.UtcNow;
        var adminId = currentAdminService.AdminUserId;
        var classification = request.Classification.ToLowerInvariant();

        // RN-005: Classify() altera apenas a classificação — o registro permanece intacto.
        alert.Classify(classification, adminId, utcNow);

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            alert.SetNote(TruncateSafe(request.Note)!, adminId, utcNow);
        }

        var safeNote = TruncateSafe(request.Note);
        var metadataSafe = safeNote is null
            ? $"{{\"alertType\":\"{alert.AlertType}\",\"classification\":\"{classification}\"}}"
            : $"{{\"alertType\":\"{alert.AlertType}\",\"classification\":\"{classification}\",\"note\":\"{safeNote}\"}}";

        await auditLogService.RecordAsync(
            AuditActions.AdminSecurityAlertClassified,
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

        return truncated.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
