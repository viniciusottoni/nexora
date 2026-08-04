namespace Nexora.Domain.Metrics;

/// <summary>
/// Catálogo dos tipos de <see cref="Alert"/> emitidos pela mesa/comanda (US-025 "Chamar garçom pela
/// mesa" e US-026 "Solicitar a conta") — <see cref="Alert.Type"/> é texto livre (ver
/// <c>AlertConfiguration</c>, sem enum nativo), então esta classe existe só para as duas camadas
/// (Application, que cria os alertas, e Api.Edge, no worker de escalonamento) compartilharem a
/// MESMA string sem replicense o literal em cada lugar — evita o tipo clássico de bug de "digitei
/// WAITER_CALLED errado num dos dois lugares e o filtro nunca casa".
/// </summary>
public static class AlertTypes
{
    /// <summary>EVT-021 <c>table.waiter_called</c> — cliente chamou o garçom pela mesa (US-025).</summary>
    public const string WaiterCalled = "WAITER_CALLED";

    /// <summary>EVT-022 <c>table.bill_requested</c> — conta solicitada, sessão em <c>BILL_REQUESTED</c> (US-026).</summary>
    public const string BillRequested = "BILL_REQUESTED";

    /// <summary>US-004/E-01 — papel de um usuário mudou (Roles/CreateRole, Roles/UpdateRole).</summary>
    public const string PermissionChanged = "PERMISSION_CHANGED";

    /// <summary>US-004/Auth — login por PIN de dispositivo não pareado.</summary>
    public const string DeviceNotRegistered = "DEVICE_NOT_REGISTERED";

    /// <summary>US-004/Auth — PIN bloqueado por excesso de tentativas.</summary>
    public const string PinLocked = "PIN_LOCKED";

    // Catálogo do motor de alertas do MVP (E-08/US-080 §2) — thresholds em
    // Nexora.Application.Alerts.Support.AlertThresholds, mensagem em AlertMessages.

    /// <summary>Pedido ultrapassou o limiar de atraso configurado (US-080, cenário "Pedido atrasado").</summary>
    public const string OrderLate = "ORDER_LATE";

    /// <summary>Tempo médio de entrega/produção da loja acima da meta prometida (US-080).</summary>
    public const string AvgTimeAboveTarget = "AVG_TIME_ABOVE_TARGET";

    /// <summary>Produto marcado indisponível (US-080; espelha EVT-051 <c>product.availability_changed</c>).</summary>
    public const string ProductUnavailable = "PRODUCT_UNAVAILABLE";

    /// <summary>Divergência de caixa acima do limiar no fechamento de uma sessão (US-080).</summary>
    public const string CashDivergence = "CASH_DIVERGENCE";

    /// <summary>Instalação edge atrasada na sincronização com a nuvem além do limiar (US-080).</summary>
    public const string SyncDelay = "SYNC_DELAY";

    /// <summary>Cancelamentos de item/pedido acima do padrão numa janela recente (US-080).</summary>
    public const string CancellationAboveThreshold = "CANCELLATION_ABOVE_THRESHOLD";

    /// <summary>Descontos concedidos acima do padrão numa janela recente (US-080).</summary>
    public const string DiscountAboveThreshold = "DISCOUNT_ABOVE_THRESHOLD";

    /// <summary>
    /// Todos os tipos avaliados pelo motor (US-080) — usados para validar chaves de configuração
    /// (thresholds/routing) e para os workers de avaliação periódica saberem o que escalonar.
    /// </summary>
    public static readonly IReadOnlyList<string> EngineTypes = new[]
    {
        OrderLate, AvgTimeAboveTarget, ProductUnavailable, CashDivergence, SyncDelay,
        CancellationAboveThreshold, DiscountAboveThreshold,
    };
}
