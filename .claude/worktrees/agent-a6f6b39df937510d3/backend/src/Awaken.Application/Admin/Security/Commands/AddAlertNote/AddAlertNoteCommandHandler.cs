using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Security.Commands.AddAlertNote;

/// <summary>US-219: adiciona/atualiza a nota de triagem de um alerta de segurança. Gera auditoria (RN-004).</summary>
public class AddAlertNoteCommandHandler(
    ISecurityAlertRepository securityAlertRepository,
    ICurrentAdminService currentAdminService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService) : IRequestHandler<AddAlertNoteCommand, Unit>
{
    private const int MaxNoteLength = 500;

    public async Task<Unit> Handle(AddAlertNoteCommand request, CancellationToken cancellationToken)
    {
        var alert = await securityAlertRepository.GetByIdAsync(request.AlertId, cancellationToken)
            ?? throw new NotFoundException(nameof(SecurityAlert), request.AlertId);

        var utcNow = dateTimeService.UtcNow;
        var adminId = currentAdminService.AdminUserId;

        var safeNote = TruncateSafe(request.Note)!;
        alert.SetNote(safeNote, adminId, utcNow);

        await auditLogService.RecordAsync(
            AuditActions.AdminSecurityAlertNoteAdded,
            adminId,
            AuditActorType.Admin,
            AuditResourceTypes.SecurityAlert,
            alert.Id,
            metadataSafe: $"{{\"alertType\":\"{alert.AlertType}\",\"note\":\"{safeNote}\"}}",
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
