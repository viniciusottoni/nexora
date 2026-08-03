using FluentValidation;

namespace Nexora.Application.Devices.Commands.DeleteDevice;

public sealed class DeleteDeviceCommandValidator : AbstractValidator<DeleteDeviceCommand>
{
    public DeleteDeviceCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("O dispositivo é obrigatório.");
    }
}
