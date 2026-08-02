using Nexora.Application.Devices;
using FluentValidation;

namespace Nexora.Application.Devices.Commands.PairDevice;

/// <summary>Porta de <c>validatePairInput</c> (<c>device-registry.ts</c>) + <c>pairDeviceRequestSchema</c> (zod).</summary>
public sealed class PairDeviceCommandValidator : AbstractValidator<PairDeviceCommand>
{
    public PairDeviceCommandValidator()
    {
        RuleFor(x => x.Code)
            .Matches(@"^\d{6}$").WithMessage("Código deve ter 6 dígitos.");

        RuleFor(x => x.Label)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Informe um nome para o dispositivo.")
            .Must(value => value.Trim().Length <= 100).WithMessage("Nome do dispositivo deve ter até 100 caracteres.");

        RuleFor(x => x.Kind)
            .Must(kind => DeviceKindMapper.AllowedKinds.Contains(kind)).WithMessage("Tipo de dispositivo inválido.");

        RuleFor(x => x.Fingerprint)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Identificação do dispositivo inválida.")
            .Must(value => value.Trim().Length <= 512).WithMessage("Identificação do dispositivo inválida.");
    }
}
