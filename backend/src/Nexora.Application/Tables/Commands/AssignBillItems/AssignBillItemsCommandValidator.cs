using FluentValidation;

namespace Nexora.Application.Tables.Commands.AssignBillItems;

public sealed class AssignBillItemsCommandValidator : AbstractValidator<AssignBillItemsCommand>
{
    public AssignBillItemsCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Selecione a comanda para dividir a conta.");

        // Lista VAZIA é sintaticamente válida (ex.: caixa abriu a tela e ainda não tocou em nenhum
        // item) — a recusa de negócio "item não atribuído" (RN-017, BILL_ITEM_NOT_ASSIGNED) é do
        // handler, não deste validador; travar aqui com "Count > 0" impediria justamente o cenário
        // Gherkin "nenhum item pode ficar sem atribuição antes de fechar" de alcançar o handler.
        RuleFor(x => x.Assignments).NotNull().WithMessage("Informe as atribuições de itens.");

        RuleForEach(x => x.Assignments).ChildRules(assignment =>
        {
            assignment.RuleFor(a => a.Person).GreaterThan(0).WithMessage("O número da pessoa precisa ser maior que zero.");
            assignment.RuleFor(a => a.ItemIds).NotNull().WithMessage("Informe os itens atribuídos a esta pessoa.");
        });
    }
}
