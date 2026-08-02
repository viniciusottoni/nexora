namespace Awaken.Domain.Entities.Inventory;

/// <summary>
/// US-230: rastreia quantas vezes um usuário usou um item em um determinado
/// período (diário ou semanal), permitindo enforcement de limites de uso.
/// </summary>
public class ItemUsageRecord
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ItemKey { get; private set; } = string.Empty;
    public DateTime PeriodStartUtc { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private ItemUsageRecord() { }

    /// <summary>
    /// Cria um novo registro de uso com UsageCount = 1.
    /// </summary>
    public static ItemUsageRecord Create(Guid userId, string itemKey, DateTime periodStart, DateTime utcNow)
    {
        return new ItemUsageRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemKey = itemKey,
            PeriodStartUtc = periodStart,
            UsageCount = 1,
            CreatedAtUtc = utcNow
        };
    }

    /// <summary>
    /// Incrementa o contador de uso para o período atual.
    /// </summary>
    public void IncrementUsage(DateTime utcNow)
    {
        UsageCount++;
        UpdatedAtUtc = utcNow;
    }
}
