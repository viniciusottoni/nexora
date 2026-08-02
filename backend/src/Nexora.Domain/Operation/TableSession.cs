using Nexora.Domain.Common;

namespace Nexora.Domain.Operation;

/// <summary>
/// Comanda — sessão de consumo aberta em uma <see cref="DiningTable"/>, do momento em que o
/// garçom abre a mesa até o fechamento da conta (RN-020). <see cref="BusinessDay"/> segue a
/// virada configurável do tenant, não a meia-noite civil (ADR-018).
/// </summary>
public sealed class TableSession
{
    private readonly List<Order> _orders = new();

    private TableSession() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid TableId { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public TableSessionStatus Status { get; private set; } = TableSessionStatus.Open;
    public short GuestCount { get; private set; } = 1;
    public Guid? WaiterId { get; private set; }
    public Guid? OpenedBy { get; private set; }
    public string OpenedSource { get; private set; } = "WAITER";
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? BillRequestedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ServiceFeeAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public short? Rating { get; private set; }
    public string? RatingComment { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public DiningTable Table { get; private set; } = null!;
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    public static TableSession Create(
        Guid tenantId,
        Guid storeId,
        Guid tableId,
        DateOnly businessDay,
        short guestCount = 1,
        Guid? waiterId = null,
        Guid? openedBy = null,
        string openedSource = "WAITER")
    {
        if (guestCount < 1)
            throw new DomainException("A comanda precisa ter pelo menos um cliente.");

        var now = DateTimeOffset.UtcNow;

        return new TableSession
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            TableId = tableId,
            BusinessDay = businessDay,
            Status = TableSessionStatus.Open,
            GuestCount = guestCount,
            WaiterId = waiterId,
            OpenedBy = openedBy,
            OpenedSource = openedSource,
            OpenedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void RequestBill()
    {
        if (Status is not TableSessionStatus.Open)
            throw new DomainException("Só é possível pedir a conta de uma comanda aberta.");

        Status = TableSessionStatus.BillRequested;
        BillRequestedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsPaid(decimal subtotal, decimal discountAmount, decimal serviceFeeAmount, decimal totalAmount)
    {
        if (Status is not TableSessionStatus.BillRequested)
            throw new DomainException("Só é possível pagar uma comanda com conta solicitada.");

        Status = TableSessionStatus.Paid;
        Subtotal = subtotal;
        DiscountAmount = discountAmount;
        ServiceFeeAmount = serviceFeeAmount;
        TotalAmount = totalAmount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Close()
    {
        if (Status is not TableSessionStatus.Paid)
            throw new DomainException("Só é possível fechar uma comanda paga.");

        Status = TableSessionStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Release()
    {
        ReleasedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Rate(short rating, string? comment)
    {
        if (rating is < 1 or > 5)
            throw new DomainException("A avaliação precisa estar entre 1 e 5.");

        Rating = rating;
        RatingComment = comment;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
