namespace Awaken.Domain.Entities.Inventory;

/// <summary>
/// US-230 RN-003: registro de idempotência por UseRequestId — garante que
/// reenviar a mesma requisição de uso de item não reaplique o efeito nem
/// consuma o item duas vezes. Guarda um resumo seguro da resposta original
/// (nunca o PayloadJson cru — pode conter dado pessoal, ver ADR-015) para
/// devolver a mesma resposta em caso de replay.
/// </summary>
public class ItemUsageRequest
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ItemKey { get; private set; } = string.Empty;
    public string UseRequestId { get; private set; } = string.Empty;
    public bool Success { get; private set; }
    public string EffectType { get; private set; } = string.Empty;
    public string? Message { get; private set; }
    public int RemainingQuantity { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ItemUsageRequest() { }

    public static ItemUsageRequest Create(
        Guid userId,
        string itemKey,
        string useRequestId,
        bool success,
        string effectType,
        string? message,
        int remainingQuantity,
        DateTime utcNow)
    {
        return new ItemUsageRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemKey = itemKey,
            UseRequestId = useRequestId,
            Success = success,
            EffectType = effectType,
            Message = message,
            RemainingQuantity = remainingQuantity,
            CreatedAtUtc = utcNow,
        };
    }
}
