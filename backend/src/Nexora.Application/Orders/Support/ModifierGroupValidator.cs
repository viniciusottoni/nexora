using Nexora.Shared.Errors;

namespace Nexora.Application.Orders.Support;

/// <summary>
/// US-030 §4/§5 — valida a escolha de modificadores de um item contra os grupos vinculados ao
/// produto (<c>ProductModifierGroup</c>): grupo obrigatório (<c>IsRequired</c>) sem nenhuma escolha
/// vira <see cref="ApiErrorCodes.ModifierGroupRequired"/>; contagem de escolhas fora do intervalo
/// <c>MinSelect</c>..<c>MaxSelect</c> vira <see cref="ApiErrorCodes.ModifierGroupSelectionInvalid"/>
/// (cenário Gherkin "Grupo de modificadores obrigatório pendente": "deve receber 422 com o grupo
/// pendente identificado... nenhum pedido deve ser criado"). Função pura (sem
/// <c>IApplicationDbContext</c>) — o handler resolve <see cref="GroupSpec"/> a partir do banco
/// (produto → <c>ProductModifierGroup</c> → <c>ModifierGroup</c> → <c>Modifier</c>) e chama isto,
/// mesmo espírito de <see cref="BusinessDayPolicy"/>/<see cref="ClockSkewPolicy"/>: testável em
/// unidade, único lugar da solution que decide esta regra.
/// </summary>
public static class ModifierGroupValidator
{
    /// <summary>Grupo de modificadores vinculado ao produto do item, já achatado para o que a validação precisa (sem entidade EF).</summary>
    public sealed record GroupSpec(
        Guid GroupId,
        string GroupName,
        short MinSelect,
        short MaxSelect,
        bool IsRequired,
        IReadOnlyCollection<Guid> ModifierIds);

    /// <summary>Primeiro grupo que falhou a validação — <c>Code</c> é um de <see cref="ApiErrorCodes.ModifierGroupRequired"/>/<see cref="ApiErrorCodes.ModifierGroupSelectionInvalid"/>.</summary>
    public sealed record Violation(string Code, Guid GroupId, string GroupName, int MinSelect, int MaxSelect, int Selected);

    /// <summary>
    /// Valida um único grupo contra os modificadores selecionados no item (contagem de opções
    /// distintas escolhidas do grupo, não a soma de <c>quantity</c> por modificador — "Escolha 1
    /// sabor entre 2" conta 1 escolha mesmo que o modificador aceite quantidade maior).
    /// </summary>
    public static Violation? ValidateGroup(GroupSpec group, IReadOnlyCollection<Guid> selectedModifierIds)
    {
        var selectedCount = selectedModifierIds.Count(id => group.ModifierIds.Contains(id));

        if (group.IsRequired && selectedCount == 0)
        {
            return new Violation(ApiErrorCodes.ModifierGroupRequired, group.GroupId, group.GroupName, group.MinSelect, group.MaxSelect, selectedCount);
        }

        if (selectedCount < group.MinSelect || selectedCount > group.MaxSelect)
        {
            return new Violation(ApiErrorCodes.ModifierGroupSelectionInvalid, group.GroupId, group.GroupName, group.MinSelect, group.MaxSelect, selectedCount);
        }

        return null;
    }

    /// <summary>Primeira violação encontrada, na ordem em que os grupos aparecem — <c>null</c> quando todos os grupos do produto estão satisfeitos.</summary>
    public static Violation? ValidateAll(IEnumerable<GroupSpec> groups, IReadOnlyCollection<Guid> selectedModifierIds)
    {
        foreach (var group in groups)
        {
            var violation = ValidateGroup(group, selectedModifierIds);
            if (violation is not null)
            {
                return violation;
            }
        }

        return null;
    }
}
