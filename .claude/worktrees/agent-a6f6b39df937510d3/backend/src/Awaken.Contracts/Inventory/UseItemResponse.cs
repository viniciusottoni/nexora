namespace Awaken.Contracts.Inventory;

/// <summary>
/// US-230: resposta ao uso de um item do inventário.
/// </summary>
public record UseItemResponse(
    string ItemKey,
    bool EffectApplied,
    string EffectType,
    int NewQuantity,
    string? CorrelationId);
