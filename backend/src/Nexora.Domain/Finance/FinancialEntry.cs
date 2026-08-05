using Nexora.Domain.Common;

namespace Nexora.Domain.Finance;

/// <summary>
/// Lançamento financeiro (receita ou despesa). A data de competência é a base do regime de
/// competência; vencimento e pagamento seguem o fluxo de caixa.
/// </summary>
public sealed class FinancialEntry
{
    private FinancialEntry() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? StoreId { get; private set; }
    public Guid? AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public FinancialEntryType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateOnly CompetenceDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public bool IsRecurring { get; private set; }

    // TODO: tipar quando o formato de recurrence for definido
    public string? Recurrence { get; private set; }

    public Guid? ParentEntryId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static FinancialEntry Create(
        Guid tenantId,
        FinancialEntryType type,
        decimal amount,
        string description,
        DateOnly competenceDate,
        Guid? storeId = null,
        Guid? accountId = null,
        Guid? categoryId = null,
        // US-052 (Múltiplas formas de pagamento) — vínculo ao fato de origem (ex.: "table_session",
        // id da comanda), exigido pelo cenário Gherkin "Receita registrada automaticamente": "deve
        // estar vinculado ao canal e à forma de pagamento". Canal/forma em si vivem em Description
        // (texto livre) até existir um relatório dedicado que precise deles estruturados.
        string? referenceType = null,
        Guid? referenceId = null,
        Guid? createdBy = null,
        DateTimeOffset? paidAt = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("A descrição do lançamento financeiro é obrigatória.");

        var now = DateTimeOffset.UtcNow;

        return new FinancialEntry
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            AccountId = accountId,
            CategoryId = categoryId,
            Type = type,
            Amount = amount,
            Description = description,
            CompetenceDate = competenceDate,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            IsRecurring = false,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            PaidAt = paidAt
        };
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
