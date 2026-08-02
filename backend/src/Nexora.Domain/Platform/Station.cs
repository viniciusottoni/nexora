using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>Praça de produção (forno, chapa, fritadeira, bar, sobremesa...).</summary>
public sealed class Station
{
    private Station() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public StationType Type { get; private set; } = StationType.Assembly;
    public short? CapacitySlots { get; private set; }
    public int? AvgCookSeconds { get; private set; }
    public short SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public Store Store { get; private set; } = null!;

    public static Station Create(Guid tenantId, Guid storeId, string code, string name, StationType type = StationType.Assembly, short sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("O código da praça é obrigatório.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da praça é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new Station
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            Code = code,
            Name = name,
            Type = type,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateCapacity(short? capacitySlots, int? avgCookSeconds)
    {
        CapacitySlots = capacitySlots;
        AvgCookSeconds = avgCookSeconds;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
