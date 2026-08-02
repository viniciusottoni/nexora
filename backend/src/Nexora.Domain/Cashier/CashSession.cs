using Nexora.Domain.Common;

namespace Nexora.Domain.Cashier;

/// <summary>
/// Sessão de caixa de uma loja: abre com um valor inicial em espécie, acumula movimentos
/// e pagamentos, e fecha com contagem física conferida contra o valor esperado (RF-CXA-02 a 05).
/// </summary>
public sealed class CashSession
{
    private readonly List<CashMovement> _movements = new();

    private CashSession() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid OperatorId { get; private set; }
    public Guid? DeviceId { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public CashSessionStatus Status { get; private set; } = CashSessionStatus.Open;
    public decimal OpeningAmount { get; private set; }
    public decimal? ExpectedAmount { get; private set; }
    public decimal? CountedAmount { get; private set; }
    public decimal? Divergence { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public Guid? AuthorizedBy { get; private set; }
    public string? Justification { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<CashMovement> Movements => _movements.AsReadOnly();

    public static CashSession Create(
        Guid tenantId,
        Guid storeId,
        Guid operatorId,
        DateOnly businessDay,
        decimal openingAmount,
        DateTimeOffset openedAt,
        Guid? deviceId = null)
    {
        if (openingAmount < 0)
            throw new DomainException("O valor de abertura do caixa não pode ser negativo.");

        var now = DateTimeOffset.UtcNow;

        return new CashSession
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            OperatorId = operatorId,
            DeviceId = deviceId,
            BusinessDay = businessDay,
            Status = CashSessionStatus.Open,
            OpeningAmount = openingAmount,
            OpenedAt = openedAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Registra o valor esperado em caixa, calculado a partir de abertura + movimentos + pagamentos em espécie.</summary>
    public void SetExpectedAmount(decimal expectedAmount)
    {
        if (Status == CashSessionStatus.Closed)
            throw new DomainException("Não é possível recalcular o valor esperado de uma sessão de caixa já fechada.");

        ExpectedAmount = expectedAmount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Move a sessão para conferência — contagem física em andamento.</summary>
    public void StartClosing()
    {
        if (Status != CashSessionStatus.Open)
            throw new DomainException("Só é possível iniciar o fechamento de uma sessão de caixa aberta.");

        Status = CashSessionStatus.Closing;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Fecha a sessão com o valor contado fisicamente, apurando a divergência contra o valor esperado.</summary>
    public void Close(Guid closedBy, decimal countedAmount, DateTimeOffset closedAt, Guid? authorizedBy = null, string? justification = null)
    {
        if (Status == CashSessionStatus.Closed)
            throw new DomainException("A sessão de caixa já está fechada.");

        if (countedAmount < 0)
            throw new DomainException("O valor contado no fechamento do caixa não pode ser negativo.");

        CountedAmount = countedAmount;
        Divergence = ExpectedAmount.HasValue ? countedAmount - ExpectedAmount.Value : null;
        Status = CashSessionStatus.Closed;
        ClosedBy = closedBy;
        ClosedAt = closedAt;
        AuthorizedBy = authorizedBy ?? AuthorizedBy;
        Justification = justification ?? Justification;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    internal void AddMovement(CashMovement movement)
    {
        if (movement.CashSessionId != Id)
            throw new DomainException("O movimento de caixa não pertence a esta sessão.");

        _movements.Add(movement);
    }

    public CashMovement RegisterMovement(CashMovementType type, decimal amount, string reason, Guid createdBy, DateTimeOffset occurredAt, Guid? authorizedBy = null)
    {
        if (Status != CashSessionStatus.Open)
            throw new DomainException("Só é possível lançar movimentos em uma sessão de caixa aberta.");

        var movement = CashMovement.Create(TenantId, Id, type, amount, reason, createdBy, occurredAt, authorizedBy);
        _movements.Add(movement);
        return movement;
    }
}
