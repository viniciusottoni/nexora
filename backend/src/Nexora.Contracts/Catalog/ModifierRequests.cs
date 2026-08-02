using System.Text.Json.Serialization;

namespace Nexora.Contracts.Catalog;

/// <summary>Corpo de <c>POST /v1/catalog/modifier-groups/{groupId}/modifiers</c> (US-012).</summary>
public sealed record CreateModifierRequest(
    string Name,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal PriceDelta,
    Guid? IngredientId,
    [property: JsonConverter(typeof(NullableMoneyJsonConverter))] decimal? Quantity,
    short SortOrder);

/// <summary>
/// Corpo de <c>PATCH /v1/catalog/modifier-groups/{groupId}/modifiers/{modifierId}</c>. Só cobre
/// <c>price_delta</c> (<c>Modifier.UpdatePrice</c>) — mesma limitação de Domain descrita em
/// <see cref="UpdateModifierGroupRequest"/>: <c>Nexora.Domain.Catalog.Modifier</c> não expõe
/// método para renomear, reordenar nem trocar o insumo associado depois de criado.
/// </summary>
public sealed record UpdateModifierRequest(
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal PriceDelta);

/// <summary>Corpo de <c>PATCH /v1/catalog/modifier-groups/{groupId}/modifiers/{modifierId}/availability</c>.</summary>
public sealed record UpdateModifierAvailabilityRequest(bool IsAvailable);
