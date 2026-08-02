namespace Nexora.Domain.Operation;

// Enums nativos do PostgreSQL (documento 00, §3), mapeados como enum nativo via
// Npgsql.MapEnum na configuração do EF Core (documento 13, §2) — não como VARCHAR + CHECK.

/// <summary>Estado do pedido, do rascunho ao fechamento.</summary>
public enum OrderStatus
{
    Draft,
    Placed,
    InProduction,
    Ready,
    Dispatched,
    Delivered,
    Closed,
    Cancelled
}

/// <summary>Estado de um item de pedido na fila de produção (KDS).</summary>
public enum OrderItemStatus
{
    Queued,
    Fired,
    InOven,
    OutOfOven,
    Ready,
    Served,
    Cancelled
}

/// <summary>Ciclo de vida de uma comanda (sessão de mesa).</summary>
public enum TableSessionStatus
{
    Open,
    BillRequested,
    Paid,
    Closed
}

/// <summary>Estado físico de uma mesa no mapa de salão.</summary>
public enum TableStatus
{
    Free,
    Occupied,
    Reserved,
    Blocked
}

/// <summary>Situação da emissão fiscal de um pedido.</summary>
public enum FiscalStatus
{
    None,
    Pending,
    Issued,
    Cancelled,
    Rejected
}
