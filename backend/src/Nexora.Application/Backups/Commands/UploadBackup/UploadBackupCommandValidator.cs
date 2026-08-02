using FluentValidation;

namespace Nexora.Application.Backups.Commands.UploadBackup;

public sealed class UploadBackupCommandValidator : AbstractValidator<UploadBackupCommand>
{
    public UploadBackupCommandValidator()
    {
        RuleFor(x => x.InstallationId).NotEmpty();

        RuleFor(x => x.BackupClass)
            .Must(v => v is "six-hour" or "daily").WithMessage("Classe de backup inválida.");

        RuleFor(x => x.ExpectedSha256)
            .Matches("^[0-9a-fA-F]{64}$").WithMessage("Hash SHA-256 inválido.");

        RuleFor(x => x.Content)
            .Must(c => c is { Length: > 0 }).WithMessage("Backup vazio.");
    }
}
