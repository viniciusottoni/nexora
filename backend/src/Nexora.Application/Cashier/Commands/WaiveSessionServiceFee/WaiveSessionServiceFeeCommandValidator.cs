using FluentValidation;

namespace Nexora.Application.Cashier.Commands.WaiveSessionServiceFee;

public sealed class WaiveSessionServiceFeeCommandValidator : AbstractValidator<WaiveSessionServiceFeeCommand>
{
    private static readonly string[] AllowedScopes = { "FULL", "PARTIAL" };

    public WaiveSessionServiceFeeCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Selecione a comanda para retirar a taxa de serviço.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("O motivo da retirada da taxa é obrigatório.");
        RuleFor(x => x.Scope)
            .NotEmpty().WithMessage("Informe o escopo da retirada.")
            .Must(s => AllowedScopes.Contains(s.Trim().ToUpperInvariant())).WithMessage("Escopo inválido — use FULL ou PARTIAL.");
        RuleFor(x => x.Person)
            .NotNull().WithMessage("Informe a pessoa para retirada parcial da taxa.")
            .When(x => x.Scope.Trim().Equals("PARTIAL", StringComparison.OrdinalIgnoreCase));
    }
}
