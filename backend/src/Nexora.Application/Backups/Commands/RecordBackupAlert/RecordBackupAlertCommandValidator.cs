using FluentValidation;

namespace Nexora.Application.Backups.Commands.RecordBackupAlert;

public sealed class RecordBackupAlertCommandValidator : AbstractValidator<RecordBackupAlertCommand>
{
    public RecordBackupAlertCommandValidator()
    {
        RuleFor(x => x.InstallationId).NotEmpty();

        RuleFor(x => x.Reason)
            .Must(v => v == "UPLOAD_FAILED").WithMessage("Motivo de alerta de backup desconhecido.");
    }
}
