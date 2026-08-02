namespace Nexora.Contracts.Catalog;

/// <summary>
/// Grupo de modificadores (ex.: "Tamanho", "Adicionais") com seus modificadores e os produtos que
/// hoje o reusam — porta de <c>modifierGroupSchema</c> (US-012). <see cref="ProductIds"/> existe
/// para a tela de gestão mostrar/editar reuso entre produtos sem precisar de uma segunda chamada
/// (RN do doc: "grupo vinculado a 12 produtos... alteração deve valer para os 12" — a lista aqui
/// é só leitura de apoio à UI, a autoridade do vínculo em si é a tabela <c>product_modifier_group</c>).
/// </summary>
public sealed record ModifierGroupResponse(
    Guid Id,
    string Name,
    short MinSelect,
    short MaxSelect,
    bool IsRequired,
    short SortOrder,
    IReadOnlyList<ModifierResponse> Modifiers,
    IReadOnlyList<Guid> ProductIds);

public sealed record ModifierGroupListResponse(IReadOnlyList<ModifierGroupResponse> Items);

/// <summary>Confirma o vínculo (ou desvínculo) entre produto e grupo de modificadores — porta de <c>POST/DELETE /v1/catalog/products/{id}/modifier-groups</c>.</summary>
public sealed record ProductModifierGroupResponse(Guid ProductId, Guid GroupId, short SortOrder);
