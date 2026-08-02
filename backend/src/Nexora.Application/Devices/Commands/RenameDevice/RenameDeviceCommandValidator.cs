using FluentValidation;

namespace Nexora.Application.Devices.Commands.RenameDevice;

public sealed class RenameDeviceCommandValidator : AbstractValidator<RenameDeviceCommand>
{
    public RenameDeviceCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("O dispositivo é obrigatório.");

        RuleFor(x => x.Label)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Informe um nome para o dispositivo.")
            .Must(value => value.Trim().Length <= 100).WithMessage("Nome do dispositivo deve ter até 100 caracteres.");
    }
}
