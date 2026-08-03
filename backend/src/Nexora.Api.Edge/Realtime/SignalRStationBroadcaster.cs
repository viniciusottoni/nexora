using Nexora.Api.Edge.Hubs;
using Nexora.Application.Abstractions.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Edge.Realtime;

/// <summary>
/// Implementação de <see cref="IStationBroadcaster"/> sobre <see cref="IHubContext{THub}"/> (US-031)
/// — réplica do MESMO padrão de <c>SignalRTableConsumptionBroadcaster</c> (US-024): mensagem
/// <c>{ type, data }</c> pelo método de cliente <c>kdsEvent</c>, salas <c>station:{id}</c>/
/// <c>role:{papel}</c>/<c>table:{id}</c> (ADR-011).
///
/// <c>OrderPlaced</c>/<c>ItemQueued</c> emitem para <c>role:cashier</c> E <c>role:waiter</c> (RN-003:
/// "cada transição de estado gera alerta aos perfis envolvidos", os dois ao mesmo tempo, mesmo
/// pedido) além da(s) praça(s) e da mesa — cenário Gherkin "Chegada ao KDS": "mesa, garçom, cozinha
/// e caixa devem receber alerta".
/// </summary>
internal sealed class SignalRStationBroadcaster : IStationBroadcaster
{
    private const string MethodName = "kdsEvent";

    private readonly IHubContext<KdsHub> _hub;

    public SignalRStationBroadcaster(IHubContext<KdsHub> hub)
    {
        _hub = hub;
    }

    public async Task OrderPlaced(
        Guid tenantId,
        Guid orderId,
        string shortCode,
        Guid? tableId,
        string? tableLabel,
        string channel,
        IReadOnlyList<StationBroadcastItem> items,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var payload = BuildOrderPayload("order.placed", orderId, shortCode, tableId, tableLabel, channel, items);

        // Cenário Gherkin "Salas corretas do WebSocket": station:{id} só recebe os itens da PRÓPRIA
        // praça — nunca a lista inteira (station:bebidas nunca vê um pedido com só item de forno).
        foreach (var stationId in items.Select(i => i.StationId).Where(id => id is not null).Distinct())
        {
            var stationPayload = BuildOrderPayload(
                "order.placed", orderId, shortCode, tableId, tableLabel, channel, items.Where(i => i.StationId == stationId).ToList());
            await _hub.Clients.Group(StationGroup(stationId!.Value)).SendAsync(MethodName, stationPayload, cancellationToken);
        }

        // Caixa e garçom acompanham o pedido INTEIRO, não só a própria praça (RN-001/RN-003).
        await _hub.Clients.Group(RoleGroup("cashier")).SendAsync(MethodName, payload, cancellationToken);
        await _hub.Clients.Group(RoleGroup("waiter")).SendAsync(MethodName, payload, cancellationToken);

        if (tableId is { } id)
        {
            await _hub.Clients.Group(TableGroup(id)).SendAsync(MethodName, payload, cancellationToken);
        }
    }

    public async Task ItemQueued(
        Guid tenantId,
        Guid orderId,
        string shortCode,
        Guid? tableId,
        string? tableLabel,
        string channel,
        StationBroadcastItem item,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var payload = BuildOrderPayload("order.item.queued", orderId, shortCode, tableId, tableLabel, channel, new[] { item });

        if (item.StationId is { } stationId)
        {
            await _hub.Clients.Group(StationGroup(stationId)).SendAsync(MethodName, payload, cancellationToken);
        }

        await _hub.Clients.Group(RoleGroup("cashier")).SendAsync(MethodName, payload, cancellationToken);
        await _hub.Clients.Group(RoleGroup("waiter")).SendAsync(MethodName, payload, cancellationToken);

        if (tableId is { } id)
        {
            await _hub.Clients.Group(TableGroup(id)).SendAsync(MethodName, payload, cancellationToken);
        }
    }

    public Task ItemStatusChanged(
        Guid tenantId, Guid stationId, Guid orderItemId, string productName, string status, CancellationToken cancellationToken) =>
        _hub.Clients.Group(StationGroup(stationId)).SendAsync(
            MethodName,
            new
            {
                type = $"order.item.{status.ToLowerInvariant()}",
                data = new { orderItemId, productName, status },
            },
            cancellationToken);

    private static object BuildOrderPayload(
        string type, Guid orderId, string shortCode, Guid? tableId, string? tableLabel, string channel, IReadOnlyList<StationBroadcastItem> items) =>
        new
        {
            type,
            data = new
            {
                orderId,
                code = shortCode,
                shortCode,
                table = tableLabel,
                tableId,
                channel,
                items = items.Select(i => new
                {
                    orderItemId = i.OrderItemId,
                    productName = i.ProductName,
                    stationId = i.StationId,
                    quantity = i.Quantity,
                    modifiers = i.Modifiers,
                    notes = i.Notes,
                }),
            },
        };

    private static string StationGroup(Guid stationId) => $"station:{stationId}";

    private static string RoleGroup(string role) => $"role:{role}";

    private static string TableGroup(Guid tableId) => $"table:{tableId}";
}
