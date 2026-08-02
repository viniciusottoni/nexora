using Nexora.Domain.Common;

namespace Nexora.Domain.Catalog;

/// <summary>Categoria do cardápio (ex.: Pizzas, Bebidas, Sobremesas) — agrupa produtos e define ordem de exibição.</summary>
public sealed class Category
{
    private readonly List<Product> _products = new();

    private Category() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public short SortOrder { get; private set; }

    // TODO: value object tipado quando o formato de availableSchedule for definido — hoje é JSONB livre
    public string? AvailableSchedule { get; private set; }

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    public static Category Create(Guid tenantId, string name, string? description = null, short sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da categoria é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new Category
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDetails(string name, string? description, short sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da categoria é obrigatório.");

        Name = name;
        Description = description;
        SortOrder = sortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateAvailableSchedule(string? availableScheduleJson)
    {
        AvailableSchedule = availableScheduleJson;
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
