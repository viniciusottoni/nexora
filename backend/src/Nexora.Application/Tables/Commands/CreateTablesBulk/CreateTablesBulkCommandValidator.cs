using FluentValidation;

namespace Nexora.Application.Tables.Commands.CreateTablesBulk;

public sealed class CreateTablesBulkCommandValidator : AbstractValidator<CreateTablesBulkCommand>
{
    /// <summary>Teto de segurança do lote — onboarding real não passa disso; acima é sinal de erro de digitação (ex.: trocar "de 1 a 20" por "de 1 a 2000").</summary>
    public const int MaxBatchSize = 200;

    public CreateTablesBulkCommandValidator()
    {
        RuleFor(x => x.AreaId).NotEmpty().WithMessage("Selecione o ambiente das mesas.");

        RuleFor(x => x.From).GreaterThan(0).WithMessage("O número inicial deve ser maior que zero.");

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From).WithMessage("O número final deve ser maior ou igual ao inicial.");

        RuleFor(x => x)
            .Must(x => x.To - x.From + 1 <= MaxBatchSize)
            .WithMessage($"O lote não pode ter mais que {MaxBatchSize} mesas de uma vez.")
            .OverridePropertyName("To");

        RuleFor(x => x.Seats).GreaterThanOrEqualTo((short)1).WithMessage("A mesa precisa ter pelo menos um assento.");
    }
}
