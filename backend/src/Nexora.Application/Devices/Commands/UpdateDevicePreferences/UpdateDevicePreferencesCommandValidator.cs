using System.Text.Json;
using FluentValidation;

namespace Nexora.Application.Devices.Commands.UpdateDevicePreferences;

public sealed class UpdateDevicePreferencesCommandValidator : AbstractValidator<UpdateDevicePreferencesCommand>
{
    public UpdateDevicePreferencesCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("O dispositivo é obrigatório.");

        RuleFor(x => x.PreferencesPatchJson)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Informe as preferências a atualizar.")
            .Must(BeAJsonObject).WithMessage("As preferências devem ser um objeto JSON.");
    }

    private static bool BeAJsonObject(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
