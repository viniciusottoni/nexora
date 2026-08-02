using Nexora.Domain.Common;

namespace Nexora.Domain.Catalog;

/// <summary>Produto do cardápio (ex.: Pizza Calabresa) — agrega variantes (tamanhos) e grupos de modificadores.</summary>
public sealed class Product
{
    private readonly List<ProductVariant> _variants = new();
    private readonly List<string> _allergens = new();
    private readonly List<ProductModifierGroup> _productModifierGroups = new();

    private Product() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid? StationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? IngredientsText { get; private set; }
    public IReadOnlyList<string> Allergens => _allergens.AsReadOnly();
    public short SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsAvailable { get; private set; } = true;
    public string? UnavailableReason { get; private set; }
    public DateTimeOffset? UnavailableSince { get; private set; }
    public bool AllowsFractions { get; private set; }
    public short MaxFractions { get; private set; } = 1;
    public string? FractionGroup { get; private set; }
    public string? Ncm { get; private set; }
    public string? Cest { get; private set; }
    public string? Cfop { get; private set; }
    public short? OriginCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public Category Category { get; private set; } = null!;
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();
    public IReadOnlyCollection<ProductModifierGroup> ProductModifierGroups => _productModifierGroups.AsReadOnly();

    public static Product Create(
        Guid tenantId,
        Guid categoryId,
        string name,
        Guid? stationId = null,
        string? description = null,
        IEnumerable<string>? allergens = null,
        bool allowsFractions = false,
        short maxFractions = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do produto é obrigatório.");

        if (maxFractions < 1)
            throw new DomainException("A quantidade máxima de frações precisa ser pelo menos 1.");

        var now = DateTimeOffset.UtcNow;

        var product = new Product
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            CategoryId = categoryId,
            StationId = stationId,
            Name = name,
            Description = description,
            AllowsFractions = allowsFractions,
            MaxFractions = maxFractions,
            IsActive = true,
            IsAvailable = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (allergens is not null)
            product._allergens.AddRange(allergens);

        return product;
    }

    public void MarkUnavailable(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo da indisponibilidade é obrigatório.");

        IsAvailable = false;
        UnavailableReason = reason;
        UnavailableSince = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAvailable()
    {
        IsAvailable = true;
        UnavailableReason = null;
        UnavailableSince = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateFiscalData(string? ncm, string? cest, string? cfop, short? originCode)
    {
        Ncm = ncm;
        Cest = cest;
        Cfop = cfop;
        OriginCode = originCode;
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
