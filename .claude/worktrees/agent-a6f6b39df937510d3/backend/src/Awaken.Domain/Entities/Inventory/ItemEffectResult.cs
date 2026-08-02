namespace Awaken.Domain.Entities.Inventory;

/// <summary>
/// US-230: resultado retornado por um <see cref="IItemEffectHandler"/> após
/// aplicar o efeito de um item.
/// </summary>
public record ItemEffectResult(
    bool Success,
    string EffectType,
    string? Message = null,
    IReadOnlyDictionary<string, object>? Data = null);
