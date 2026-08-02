namespace Nexora.Contracts.Catalog;

/// <summary>Corpo de <c>POST /v1/catalog/modifier-groups</c> (US-012).</summary>
public sealed record CreateModifierGroupRequest(
    string Name,
    short MinSelect,
    short MaxSelect,
    bool IsRequired,
    short SortOrder);

/// <summary>
/// Corpo de <c>PATCH /v1/catalog/modifier-groups/{id}</c>. Só cobre mínimo/máximo de seleção
/// (<c>ModifierGroup.UpdateSelectionRange</c>) — <c>Nexora.Domain.Catalog.ModifierGroup</c> não
/// expõe método para renomear nem para alternar <c>IsRequired</c> depois de criado (só no
/// construtor <c>Create</c>). Como Domain está fora do escopo desta tarefa (US-012 roda em
/// worktree isolado, Domain é dado como pronto), renomear/tornar opcional um grupo já existente
/// exige recriar o grupo por ora — ver relatório da tarefa, recomenda-se adicionar
/// <c>ModifierGroup.Rename(name)</c>/<c>SetRequired(bool)</c> ao Domain quando alguém puder tocá-lo.
/// </summary>
public sealed record UpdateModifierGroupRequest(short MinSelect, short MaxSelect);

/// <summary>Corpo de <c>POST /v1/catalog/products/{productId}/modifier-groups</c>.</summary>
public sealed record LinkModifierGroupToProductRequest(Guid GroupId, short SortOrder);
