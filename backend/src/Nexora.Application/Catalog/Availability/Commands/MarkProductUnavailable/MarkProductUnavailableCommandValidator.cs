using FluentValidation;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Availability.Commands.MarkProductUnavailable;

/// <summary>
/// US-044 §10: "motivo escolhido por número (1 acabou, 2 equipamento, 3 qualidade), não por
/// texto" — a US-015 original aceitava texto livre (até 200 caracteres); esta história fecha essa
/// lacuna restringindo <see cref="MarkProductUnavailableCommand.Reason"/> aos três valores de
/// <see cref="ProductUnavailableReasons.All"/>. Vale para os dois processos que batem neste
/// validator (KDS via <c>Nexora.Api.Edge</c> e painel via <c>Nexora.Api.Cloud</c>) — mesmo espírito
/// de "regra de negócio idêntica no edge e na nuvem" do restante da solution.
/// </summary>
public sealed class MarkProductUnavailableCommandValidator : AbstractValidator<MarkProductUnavailableCommand>
{
    public MarkProductUnavailableCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Selecione um produto.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Informe o motivo da indisponibilidade.")
            .Must(ProductUnavailableReasons.IsValid)
            .WithMessage("Motivo inválido. Escolha um motivo da lista: 1 acabou, 2 equipamento, 3 qualidade.")
            // ApplyConditionTo.CurrentValidator: sem isso, o .When() se aplicaria a TODA a cadeia
            // da regra (também ao NotEmpty acima) — motivo vazio pularia as duas validações e o
            // comando passaria como válido (bug reproduzido por
            // Recusa_Motivo_Vazio_Com_A_Mensagem_De_Motivo_Obrigatorio_Nao_A_De_Lista_Invalida).
            .When(x => !string.IsNullOrEmpty(x.Reason), ApplyConditionTo.CurrentValidator);
    }
}
