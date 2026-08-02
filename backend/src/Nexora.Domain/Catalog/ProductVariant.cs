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
    public short? WarnMinutes { get; private set; }
    public short? CriticalMinutes { get; private set; }
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

    /// <summary>
    /// Atualiza os campos de cadastro da variante (US-011) — nome, SKU e <see cref="SizeCode"/>.
    /// Deliberadamente não toca <see cref="PrepMinutes"/>/<see cref="WarnMinutes"/>/
    /// <see cref="CriticalMinutes"/> (escopo de <see cref="UpdatePrepTimeThresholds"/>, US-016) nem
    /// <see cref="IsActive"/>/<see cref="IsDefault"/> (ações dedicadas e auditáveis à parte).
    /// </summary>
    public void UpdateDetails(string name, string? sku, string? sizeCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da variante é obrigatório.");

        Name = name;
        Sku = sku;
        SizeCode = sizeCode;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Limiares de alerta do KDS (US-016) — quando nulos, o tenant usa o padrão global configurado.</summary>
    public void UpdatePrepTimeThresholds(short prepMinutes, short? warnMinutes, short? criticalMinutes)
    {
        if (prepMinutes < 0)
            throw new DomainException("O tempo de preparo não pode ser negativo.");

        if (warnMinutes is not null && warnMinutes < prepMinutes)
            throw new DomainException("O limiar de atenção não pode ser menor que o tempo de preparo.");

        if (criticalMinutes is not null)
        {
            var criticalFloor = warnMinutes ?? prepMinutes;
            if (criticalMinutes < criticalFloor)
                throw new DomainException("O limiar crítico não pode ser menor que o limiar de atenção.");
        }

        PrepMinutes = prepMinutes;
        WarnMinutes = warnMinutes;
        CriticalMinutes = criticalMinutes;
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
