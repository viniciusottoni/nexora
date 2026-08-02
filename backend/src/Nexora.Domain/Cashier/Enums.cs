namespace Nexora.Domain.Cashier;

// Enums nativos do PostgreSQL (documento 00, §3), mapeados como enum nativo via
// Npgsql.MapEnum na configuração do EF Core (documento 13, §2) — não como VARCHAR + CHECK.

/// <summary>Ciclo de vida de uma sessão de caixa (abertura, fechamento em conferência, fechada).</summary>
public enum CashSessionStatus
{
    Open,
    Closing,
    Closed
}

/// <summary>Direção de um lançamento manual de caixa fora do fluxo de pagamento.</summary>
public enum CashMovementType
{
    Withdrawal,
    Supply
}

/// <summary>Forma de pagamento aceita.</summary>
public enum PaymentMethod
{
    Cash,
    Credit,
    Debit,
    Pix,
    Online,
    Voucher,
    Other
}

/// <summary>Ciclo de vida de um pagamento.</summary>
public enum PaymentStatus
{
    Pending,
    Authorized,
    Paid,
    Refunded,
    Failed,
    Cancelled
}
