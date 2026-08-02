using FluentValidation;

namespace Nexora.Application.Devices.Commands.RevokeDevice;

public sealed class RevokeDeviceCommandValidator : AbstractValidator<RevokeDeviceCommand>
{
    public RevokeDeviceCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("O dispositivo é obrigatório.");
    }
}
