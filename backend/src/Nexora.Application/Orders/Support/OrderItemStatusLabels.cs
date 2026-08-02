using Nexora.Domain.Operation;

namespace Nexora.Application.Orders.Support;

/// <summary>
/// Traduz <see cref="OrderItemStatus"/> (vocabulário técnico da máquina de estado do KDS) para a
/// linguagem do cliente do salão (US-024 §3.1: "Status por item traduzido para linguagem do
/// cliente ('na fila', 'sendo preparado', 'a caminho')" — cenário Gherkin "Visualização do
/// consumo"). O valor técnico continua sendo devolvido em <c>status</c> (mesmo vocabulário de
/// <c>packages/ui/src/components/status-pill.tsx</c>, para o frontend colorir com a paleta
/// canônica já existente via <c>StatusPill</c>) — só <c>statusLabel</c> usa este mapeamento.
/// </summary>
public static class OrderItemStatusLabels
{
    /// <summary>
    /// "Sendo preparado" cobre FIRED/IN_OVEN/OUT_OF_OVEN — o cliente não precisa saber que o item
    /// está especificamente no forno ou já saiu dele, só que a cozinha já começou (reduz ansiedade
    /// sem expor detalhe operacional interno, US-024 §10).
    /// </summary>
    public static string ClientLabel(OrderItemStatus status) => status switch
    {
        OrderItemStatus.Queued => "Na fila",
        OrderItemStatus.Fired or OrderItemStatus.InOven or OrderItemStatus.OutOfOven => "Sendo preparado",
        OrderItemStatus.Ready => "A caminho",
        OrderItemStatus.Served => "Servido",
        OrderItemStatus.Cancelled => "Cancelado",
        _ => status.ToString(),
    };

    /// <summary>
    /// Nome de fio idêntico ao tipo <c>StatusPillStatus</c> de <c>packages/ui/src/components/status-pill.tsx</c>
    /// (com <c>_</c>, ex. <c>OUT_OF_OVEN</c>) — <c>status.ToString().ToUpperInvariant()</c> sozinho
    /// produziria <c>OUTOFOVEN</c> (sem separador), que não bate com nenhuma chave do componente.
    /// </summary>
    public static string ToWireStatus(OrderItemStatus status) => status switch
    {
        OrderItemStatus.Queued => "QUEUED",
        OrderItemStatus.Fired => "FIRED",
        OrderItemStatus.InOven => "IN_OVEN",
        OrderItemStatus.OutOfOven => "OUT_OF_OVEN",
        OrderItemStatus.Ready => "READY",
        OrderItemStatus.Served => "SERVED",
        OrderItemStatus.Cancelled => "CANCELLED",
        _ => status.ToString().ToUpperInvariant(),
    };

    /// <summary>Tipo do evento de tempo real (US-024 §7: <c>order.item.ready</c> etc.) — mesmo formato consumido por <c>IOrderConsumptionBroadcaster.ItemStatusChanged</c>.</summary>
    public static string ToRealtimeEventType(OrderItemStatus status) => status switch
    {
        OrderItemStatus.Fired => "order.item.fired",
        OrderItemStatus.InOven => "order.item.in_oven",
        OrderItemStatus.OutOfOven => "order.item.out_of_oven",
        OrderItemStatus.Ready => "order.item.ready",
        OrderItemStatus.Served => "order.item.served",
        OrderItemStatus.Cancelled => "order.item.cancelled",
        _ => "order.item.queued",
    };
}
