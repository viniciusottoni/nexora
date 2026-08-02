using Nexora.Domain.Common;

namespace Nexora.Domain.Operation;

/// <summary>Mesa física do salão — estado de ocupação controlado por <see cref="TableStatus"/>.</summary>
public sealed class DiningTable
{
    private DiningTable() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid AreaId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public short Seats { get; private set; } = 4;
    public string QrToken { get; private set; } = string.Empty;
    public TableStatus Status { get; private set; } = TableStatus.Free;
    public short SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public Area Area { get; private set; } = null!;

    public static DiningTable Create(Guid tenantId, Guid storeId, Guid areaId, string label, string qrToken, short seats = 4, short sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("O identificador da mesa é obrigatório.");

        if (string.IsNullOrWhiteSpace(qrToken))
            throw new DomainException("O token de QR Code da mesa é obrigatório.");

        if (seats < 1)
            throw new DomainException("A mesa precisa ter pelo menos um assento.");

        var now = DateTimeOffset.UtcNow;

        return new DiningTable
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            AreaId = areaId,
            Label = label,
            QrToken = qrToken,
            Seats = seats,
            Status = TableStatus.Free,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Occupy()
    {
        if (Status is TableStatus.Blocked)
            throw new DomainException("Mesa bloqueada não pode ser ocupada.");

        Status = TableStatus.Occupied;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Release()
    {
        Status = TableStatus.Free;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reserve()
    {
        if (Status is TableStatus.Occupied)
            throw new DomainException("Mesa ocupada não pode ser reservada.");

        Status = TableStatus.Reserved;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Block()
    {
        Status = TableStatus.Blocked;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
