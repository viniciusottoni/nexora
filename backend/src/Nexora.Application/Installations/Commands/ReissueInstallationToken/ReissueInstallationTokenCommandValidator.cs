using FluentValidation;

namespace Nexora.Application.Installations.Commands.ReissueInstallationToken;

public sealed class ReissueInstallationTokenCommandValidator : AbstractValidator<ReissueInstallationTokenCommand>
{
    /// <summary>
    /// US-156 "validade configurável com limite máximo seguro (ex.: não deixar pedir 30 dias)" —
    /// 72h é o mesmo teto já usado neste código-base para um token/convite de curta duração emitido
    /// por um administrador de plataforma (<c>OwnerInviteTtl</c> em
    /// <c>ProvisionTenantCommandHandler</c>); um comando de instalação que precisar de mais tempo
    /// deve ser reemitido de novo, não estender uma janela única por semanas.
    /// </summary>
    public const int MaxExpiresInHours = 72;

    public ReissueInstallationTokenCommandValidator()
    {
        RuleFor(x => x.InstallationId).NotEmpty().WithMessage("O id da instalação é obrigatório.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("O motivo da reemissão é obrigatório.")
            .MaximumLength(500).WithMessage("O motivo da reemissão deve ter no máximo 500 caracteres.");

        RuleFor(x => x.ExpiresInHours)
            .InclusiveBetween(1, MaxExpiresInHours)
            .WithMessage($"A validade do token deve ser entre 1 e {MaxExpiresInHours} horas.");
    }
}
