using Nexora.Domain.Common;

namespace Nexora.Domain.Catalog;

/// <summary>Variante de um produto (ex.: tamanho Grande, Broto) — unidade real de venda e de preço.</summary>
public sealed class ProductVariant
{
    private readonly List<Price> _prices = new();

    private ProductVariant() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Sku { get; private set; }
    public string? SizeCode { get; private set; }
    public short PrepMinutes { get; private set; } = 10;
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;

    // TODO: value object tipado quando o formato de fiscalRates for definido — hoje é JSONB livre
    public string? FiscalRates { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public Product Product { get; private set; } = null!;
    public IReadOnlyCollection<Price> Prices => _prices.AsReadOnly();

    public static ProductVariant Create(
        Guid tenantId,
        Guid productId,
        string name,
        short prepMinutes = 10,
        bool isDefault = false,
        string? sku = null,
        string? sizeCode = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da variante é obrigatório.");

        if (prepMinutes < 0)
            throw new DomainException("O tempo de preparo não pode ser negativo.");

        var now = DateTimeOffset.UtcNow;

        return new ProductVariant
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            ProductId = productId,
            Name = name,
            Sku = sku,
            SizeCode = sizeCode,
            PrepMinutes = prepMinutes,
            IsDefault = isDefault,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateFiscalRates(string? fiscalRatesJson)
    {
        FiscalRates = fiscalRatesJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UnmarkAsDefault()
    {
        IsDefault = false;
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
