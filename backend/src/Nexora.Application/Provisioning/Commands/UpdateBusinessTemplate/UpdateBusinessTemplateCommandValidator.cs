using System.Text.Json;
using FluentValidation;

namespace Nexora.Application.Provisioning.Commands.UpdateBusinessTemplate;

public sealed class UpdateBusinessTemplateCommandValidator : AbstractValidator<UpdateBusinessTemplateCommand>
{
    public UpdateBusinessTemplateCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Informe o código do modelo de negócio.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Informe o nome do modelo de negócio.")
            .MaximumLength(120).WithMessage("O nome deve ter no máximo 120 caracteres.");

        RuleFor(x => x.ConfigJson)
            .NotEmpty().WithMessage("Informe a configuração do modelo.")
            .Must(BeValidJson).WithMessage("A configuração precisa ser um JSON válido.");

        RuleFor(x => x.SeedsJson)
            .NotEmpty().WithMessage("Informe os seeds do modelo.")
            .Must(BeValidJson).WithMessage("Os seeds precisam ser um JSON válido.");
    }

    private static bool BeValidJson(string value)
    {
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
