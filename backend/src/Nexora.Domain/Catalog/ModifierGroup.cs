using Nexora.Domain.Common;

namespace Nexora.Domain.Catalog;

/// <summary>
/// Grupo de modificadores (ex.: "Escolha o ponto da carne", "Adicionais") — reutilizável
/// entre produtos via <see cref="ProductModifierGroup"/>.
/// </summary>
public sealed class ModifierGroup
{
    private readonly List<Modifier> _modifiers = new();
    private readonly List<ProductModifierGroup> _productModifierGroups = new();

    private ModifierGroup() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public short MinSelect { get; private set; }
    public short MaxSelect { get; private set; } = 1;
    public bool IsRequired { get; private set; }
    public short SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public IReadOnlyCollection<Modifier> Modifiers => _modifiers.AsReadOnly();
    public IReadOnlyCollection<ProductModifierGroup> ProductModifierGroups => _productModifierGroups.AsReadOnly();

    public static ModifierGroup Create(Guid tenantId, string name, short minSelect = 0, short maxSelect = 1, bool isRequired = false, short sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do grupo de modificadores é obrigatório.");

        if (minSelect < 0)
            throw new DomainException("A quantidade mínima de seleção não pode ser negativa.");

        if (maxSelect < minSelect)
            throw new DomainException("A quantidade máxima de seleção não pode ser menor que a mínima.");

        var now = DateTimeOffset.UtcNow;

        return new ModifierGroup
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Name = name,
            MinSelect = minSelect,
            MaxSelect = maxSelect,
            IsRequired = isRequired,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateSelectionRange(short minSelect, short maxSelect)
    {
        if (minSelect < 0)
            throw new DomainException("A quantidade mínima de seleção não pode ser negativa.");

        if (maxSelect < minSelect)
            throw new DomainException("A quantidade máxima de seleção não pode ser menor que a mínima.");

        MinSelect = minSelect;
        MaxSelect = maxSelect;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
