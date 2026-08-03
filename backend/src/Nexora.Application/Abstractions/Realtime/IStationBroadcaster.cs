namespace Nexora.Application.Abstractions.Realtime;

/// <summary>
/// Item de um pedido, na forma exigida pelo contrato de tempo real da US-031 §7 (mensagem
/// <c>order.placed</c>): <c>stationId</c> decide para qual sala <c>station:{id}</c> este item é
/// replicado (<see cref="IStationBroadcaster.OrderPlaced"/> filtra por praça — quem está na sala da
/// praça Bebidas nunca recebe um item cujo <see cref="StationId"/> é a praça Forno, cenário Gherkin
/// "Salas corretas do WebSocket").
/// </summary>
public sealed record StationBroadcastItem(
    Guid OrderItemId,
    string ProductName,
    Guid? StationId,
    short Quantity,
    IReadOnlyList<string> Modifiers,
    string? Notes);

/// <summary>
/// Porta de roteamento em tempo real de pedido/item por PRAÇA de produção (US-031, ADR-011) —
/// diferente de <see cref="IOrderConsumptionBroadcaster"/> (sala <c>table:{id}</c>, o que o CLIENTE
/// da mesa vê) e de <see cref="IAlertsBroadcaster"/> (alerta pontual a garçom/caixa): este
/// broadcaster é o que leva o pedido confirmado à FILA de quem vai prepará-lo — a materialização de
/// RN-001 ("todo pedido confirmado é roteado simultaneamente para cozinha e caixa").
///
/// Salas emitidas (ADR-011 §"Salas", derivadas dos claims do token, nunca solicitadas pelo cliente):
/// <c>station:{id}</c> (só quem produz aquele item), <c>role:cashier</c> e <c>role:waiter</c> (todo
/// caixa/garçom do ambiente vê o pedido inteiro), <c>table:{id}</c> quando há mesa (reaproveita o
/// mesmo grupo de <see cref="IOrderConsumptionBroadcaster"/>/<c>TableConsumptionHub"/>, ver
/// <c>SignalRStationBroadcaster</c>).
///
/// Implementada com SignalR em <c>Nexora.Api.Edge</c> (<c>Realtime.SignalRStationBroadcaster</c> +
/// <c>Hubs.KdsHub</c>) — <c>Application</c> nunca referencia SignalR (ADR-039). Chamada SEMPRE de
/// dentro do handler, antes deste retornar (mesmo padrão síncrono de
/// <see cref="IOrderConsumptionBroadcaster"/>/<see cref="IAvailabilityBroadcaster"/>).
/// </summary>
public interface IStationBroadcaster
{
    /// <summary>
    /// EVT-002 <c>order.placed</c> (US-031 §7) — chamado por <c>CreateOrderCommandHandler</c> depois
    /// que TODOS os itens do pedido nasceram <c>QUEUED</c>. Um único pedido pode ter itens de praças
    /// diferentes (ex.: pizza no forno, refrigerante no bar) — a implementação filtra
    /// <paramref name="items"/> por praça antes de emitir para cada <c>station:{id}</c>, mas emite a
    /// lista INTEIRA para <c>role:cashier</c>/<c>role:waiter</c>/<c>table:{id}</c> (quem acompanha o
    /// pedido todo, não só a própria praça).
    /// </summary>
    Task OrderPlaced(
        Guid tenantId,
        Guid orderId,
        string shortCode,
        Guid? tableId,
        string? tableLabel,
        string channel,
        IReadOnlyList<StationBroadcastItem> items,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// EVT-004 <c>order.item.queued</c> (US-031 §6/§7) — item lançado DEPOIS da criação do pedido
    /// (<c>AddOrderItemCommandHandler</c>, mesa já com pedido aberto) nasce <c>QUEUED</c> e precisa
    /// do MESMO roteamento por praça de <see cref="OrderPlaced"/>, só que para um item isolado.
    /// </summary>
    Task ItemQueued(
        Guid tenantId,
        Guid orderId,
        string shortCode,
        Guid? tableId,
        string? tableLabel,
        string channel,
        StationBroadcastItem item,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Mudança de status de um item já na fila (<c>AdvanceOrderItemStatusCommandHandler</c>) — só
    /// <c>station:{id}</c> da PRÓPRIA praça precisa saber que o item saiu/avançou na fila dela;
    /// caixa/garçom/mesa já são avisados pelo <see cref="IOrderConsumptionBroadcaster"/> existente
    /// (US-024) — este método não duplica aquele aviso.
    /// </summary>
    Task ItemStatusChanged(
        Guid tenantId,
        Guid stationId,
        Guid orderItemId,
        string productName,
        string status,
        CancellationToken cancellationToken);
}
