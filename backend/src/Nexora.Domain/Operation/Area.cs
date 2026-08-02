using Nexora.Domain.Common;

namespace Nexora.Domain.Operation;

/// <summary>Área do salão (ex.: Térreo, Varanda) — agrupa mesas para o mapa de salão.</summary>
public sealed class Area
{
    private readonly List<DiningTable> _tables = new();

    private Area() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public short SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public IReadOnlyCollection<DiningTable> Tables => _tables.AsReadOnly();

    public static Area Create(Guid tenantId, Guid storeId, string name, short sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da área é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new Area
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            Name = name,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Rename(string name, short sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da área é obrigatório.");

        Name = name;
        SortOrder = sortOrder;
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
