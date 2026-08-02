using Nexora.Domain.Common;

namespace Nexora.Domain.Inventory;

/// <summary>
/// Insumo controlado em estoque. <see cref="CurrentStock"/> é uma projeção materializada
/// para leitura rápida — a regra de ouro do produto é que o saldo de estoque nunca é
/// escrito diretamente pelo domínio: ele é sempre derivado da soma de <see cref="StockMovement"/>
/// (ADR-008). Por isso esta entidade não expõe nenhum método que altere o saldo; quem
/// materializa <see cref="CurrentStock"/> e <see cref="StockSyncedAt"/> é o processo de
/// projeção/sincronização, na camada de infraestrutura.
/// </summary>
public sealed class Ingredient
{
    private Ingredient() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Category { get; private set; }
    public string UomCode { get; private set; } = string.Empty;
    public Guid? SupplierId { get; private set; }
    public decimal AvgCost { get; private set; }
    public decimal? LastCost { get; private set; }
    public decimal CurrentStock { get; private set; }
    public DateTimeOffset? StockSyncedAt { get; private set; }
    public decimal MinStock { get; private set; }
    public bool IsPerishable { get; private set; }
    public int? ShelfLifeDays { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static Ingredient Create(
        Guid tenantId,
        string name,
        string uomCode,
        Guid? supplierId = null,
        string? category = null,
        decimal minStock = 0,
        bool isPerishable = false,
        int? shelfLifeDays = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do insumo é obrigatório.");

        if (string.IsNullOrWhiteSpace(uomCode))
            throw new DomainException("A unidade de medida do insumo é obrigatória.");

        if (minStock < 0)
            throw new DomainException("O estoque mínimo do insumo não pode ser negativo.");

        if (shelfLifeDays is < 0)
            throw new DomainException("O prazo de validade do insumo não pode ser negativo.");

        var now = DateTimeOffset.UtcNow;

        return new Ingredient
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Name = name,
            Category = category,
            UomCode = uomCode,
            SupplierId = supplierId,
            AvgCost = 0,
            CurrentStock = 0,
            MinStock = minStock,
            IsPerishable = isPerishable,
            ShelfLifeDays = shelfLifeDays,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Atualiza o custo médio e o último custo — chamado pela infraestrutura ao processar uma compra.</summary>
    public void UpdateCost(decimal avgCost, decimal lastCost)
    {
        if (avgCost < 0 || lastCost < 0)
            throw new DomainException("O custo do insumo não pode ser negativo.");

        AvgCost = avgCost;
        LastCost = lastCost;
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
