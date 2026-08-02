using Nexora.Domain.Common;

namespace Nexora.Domain.Delivery;

/// <summary>
/// Parada de entrega — uma por pedido — dentro de uma rota (<see cref="DeliveryRun"/>).
/// Status e desfecho alimentam a métrica de entrega (EVT-*, MET-*).
/// </summary>
public sealed class DeliveryStop
{
    private DeliveryStop() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? RunId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid? AddressId { get; private set; }
    public int Sequence { get; private set; } = 1;
    public DeliveryStopStatus Status { get; private set; } = DeliveryStopStatus.Pending;
    public DateTimeOffset? AssignedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DeliveryOutcome? Outcome { get; private set; }
    public string? OutcomeReason { get; private set; }
    public string? ReceivedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static DeliveryStop Create(Guid tenantId, Guid orderId, Guid? addressId = null, int sequence = 1)
    {
        var now = DateTimeOffset.UtcNow;

        return new DeliveryStop
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            OrderId = orderId,
            AddressId = addressId,
            Sequence = sequence,
            Status = DeliveryStopStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Assign(Guid runId)
    {
        RunId = runId;
        Status = DeliveryStopStatus.Assigned;
        AssignedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deliver()
    {
        Status = DeliveryStopStatus.Delivered;
        Outcome = DeliveryOutcome.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(DeliveryOutcome outcome, string? outcomeReason = null)
    {
        Status = DeliveryStopStatus.Failed;
        Outcome = outcome;
        OutcomeReason = outcomeReason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
