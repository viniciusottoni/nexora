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

/// <summary>
/// US-058 (Registrar pagamento de maquininha externa) — estado de conciliação contra o extrato do
/// provedor (RF-CXA-11, Fase 3, fora de escopo do MVP; aqui só a estrutura é preparada, ADR-024).
/// </summary>
public enum PaymentReconciliationStatus
{
    /// <summary>Pagamento sem provedor externo (dinheiro, ou forma sem conciliação aplicável) — nunca entra na fila de conciliação.</summary>
    NotApplicable,

    /// <summary>Pagamento com provedor externo (maquininha) aguardando conciliação contra o extrato.</summary>
    Pending,

    /// <summary>Conciliado contra o extrato do provedor (Fase 3 — nenhum fluxo desta wave marca este estado ainda).</summary>
    Reconciled
}
