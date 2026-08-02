using FluentValidation;

namespace Nexora.Application.Auth.Commands.LoginWithPin;

/// <summary>Forma do corpo — porta de pinLoginSchema (packages/contracts/src/auth.ts). O X-Device-Secret é checado no handler (é um DEVICE_NOT_REGISTERED de negócio, não erro de validação de forma).</summary>
public sealed class LoginWithPinCommandValidator : AbstractValidator<LoginWithPinCommand>
{
    public LoginWithPinCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("O dispositivo é obrigatório.");

        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage("Informe o PIN.")
            .Matches(@"^\d{4,6}$").WithMessage("O PIN deve ter de 4 a 6 dígitos.");
    }
}
