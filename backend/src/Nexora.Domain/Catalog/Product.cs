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
    public bool AutoRestoreNextDay { get; private set; } = true;
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

    /// <summary>
    /// Atualiza os campos de cadastro do produto (US-010) — nome, categoria/praça vinculadas,
    /// descrição, ingredientes visíveis ao cliente, alérgenos, fracionamento e posição de exibição.
    /// Não altera <see cref="IsActive"/> nem <see cref="IsAvailable"/>: ativação/desativação e
    /// indisponibilidade operacional têm métodos e endpoints dedicados, por serem ações distintas
    /// de uma edição de campo (US-010 §3.1).
    /// </summary>
    public void UpdateDetails(
        string name,
        Guid categoryId,
        Guid? stationId,
        string? description,
        string? ingredientsText,
        IEnumerable<string>? allergens,
        bool allowsFractions,
        short maxFractions,
        short sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do produto é obrigatório.");

        if (maxFractions < 1)
            throw new DomainException("A quantidade máxima de frações precisa ser pelo menos 1.");

        Name = name;
        CategoryId = categoryId;
        StationId = stationId;
        Description = description;
        IngredientsText = ingredientsText;

        _allergens.Clear();
        if (allergens is not null)
            _allergens.AddRange(allergens);

        AllowsFractions = allowsFractions;
        MaxFractions = maxFractions;
        SortOrder = sortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Reposiciona o produto dentro da categoria (arrastar e soltar, US-010 §4, cenário "Ordenação do cardápio").</summary>
    public void UpdateSortOrder(short sortOrder)
    {
        SortOrder = sortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkUnavailable(string reason, bool autoRestoreNextDay = true)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo da indisponibilidade é obrigatório.");

        IsAvailable = false;
        UnavailableReason = reason;
        UnavailableSince = DateTimeOffset.UtcNow;
        AutoRestoreNextDay = autoRestoreNextDay;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAvailable()
    {
        IsAvailable = true;
        UnavailableReason = null;
        UnavailableSince = null;
        AutoRestoreNextDay = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// US-016 — reatribui a praça de produção de um produto já criado (antes só era possível
    /// definir a praça na criação, via <see cref="Create"/>). Aceita <c>null</c> para remover o
    /// vínculo (produto sem praça definida). Sem invariante adicional aqui: a existência da
    /// praça e o isolamento por tenant são responsabilidade da Application (o Domain não
    /// consulta <c>Station</c>).
    /// </summary>
    public void ReassignStation(Guid? stationId)
    {
        StationId = stationId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// US-013 (Pizza meio a meio) — define o grupo de fracionamento do produto: só produtos do
    /// MESMO grupo podem ser combinados em frações de um item (ex.: todas as pizzas salgadas no
    /// grupo "PIZZA"; um produto de grupo "HAMBURGUER" nunca combina com uma pizza). Método
    /// dedicado, em vez de estender <see cref="UpdateDetails"/>, porque nenhuma US anterior
    /// (US-010/US-011) expôs escrita deste campo — esta é a primeira funcionalidade que
    /// realmente consome <see cref="FractionGroup"/>, e ele é lido por
    /// <c>Nexora.Application.Catalog.FractionPricing.FractionPricingCalculator</c>. <c>null</c>
    /// é aceito (produto sem grupo definido não combina com nenhum outro em fração).
    /// </summary>
    public void SetFractionGroup(string? fractionGroup)
    {
        FractionGroup = fractionGroup;
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
