namespace Nexora.Domain.Delivery;

// Enums nativos do PostgreSQL (documento 00, §3), mapeados como enum nativo via
// Npgsql.MapEnum na configuração do EF Core (documento 13, §2) — não como VARCHAR + CHECK.

/// <summary>Estado de uma parada de entrega dentro de uma rota (<see cref="DeliveryRun"/>).</summary>
public enum DeliveryStopStatus
{
    Pending,
    Assigned,
    InTransit,
    Delivered,
    Failed
}

/// <summary>Desfecho registrado ao concluir (ou falhar) uma parada de entrega.</summary>
public enum DeliveryOutcome
{
    Delivered,
    CustomerAbsent,
    WrongAddress,
    Refused,
    Other
}
