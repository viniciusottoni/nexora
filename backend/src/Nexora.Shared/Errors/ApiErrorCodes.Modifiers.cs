namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do módulo de grupos de modificadores e modificadores (US-012, ADR-021).
/// Cobre <c>ModifierGroup</c>, <c>Modifier</c> e o vínculo N:N <c>ProductModifierGroup</c>
/// (<c>Nexora.Domain.Catalog</c>). Mapeamento código → status HTTP fica em
/// <c>Nexora.Api.Cloud/Edge.Infrastructure.ResultExtensions</c> — este arquivo só declara os
/// códigos; ver relatório da tarefa para os casos exatos que faltam entrar em
/// <c>ResultExtensions.MapErrorCode</c> (não editado aqui de propósito: é um arquivo central
/// reescrito por uma única pessoa depois que todas as USes em paralelo terminarem).
/// </summary>
public static partial class ApiErrorCodes
{
    /// <summary>Grupo de modificadores não encontrado (ou de outro tenant — 404, nunca 403, ADR-021).</summary>
    public const string ModifierGroupNotFound = "MODIFIER_GROUP_NOT_FOUND";

    /// <summary>Modificador (opção) não encontrado dentro do grupo informado.</summary>
    public const string ModifierNotFound = "MODIFIER_NOT_FOUND";

    /// <summary>Produto referenciado por um vínculo de grupo de modificadores não existe (ou é de outro tenant).</summary>
    public const string ModifierGroupProductNotFound = "MODIFIER_GROUP_PRODUCT_NOT_FOUND";

    /// <summary>Tentativa de vincular um grupo a um produto que já tem esse grupo vinculado.</summary>
    public const string ProductModifierGroupAlreadyLinked = "PRODUCT_MODIFIER_GROUP_ALREADY_LINKED";

    /// <summary>Tentativa de desvincular um grupo que não está vinculado ao produto informado.</summary>
    public const string ProductModifierGroupNotLinked = "PRODUCT_MODIFIER_GROUP_NOT_LINKED";

    /// <summary>Insumo (<c>ingredient_id</c>) informado num modificador não existe no tenant — ADR-021, 404 nunca 403.</summary>
    public const string ModifierIngredientNotFound = "MODIFIER_INGREDIENT_NOT_FOUND";
}
