namespace Nexora.Application.Abstractions.Realtime;

/// <summary>
/// Porta de propagação em tempo real do consumo de uma mesa (US-024, ADR-011) — sala
/// <c>table:{id}</c> (mesmo formato de sala do ADR-011 e da docstring do contrato da história:
/// <c># WebSocket, sala table:{id}:</c>). Implementada com SignalR em <c>Nexora.Api.Edge</c>
/// (<c>Realtime.SignalRTableConsumptionBroadcaster</c> + <c>Hubs.TableConsumptionHub</c>), réplica
/// do mesmo padrão de <see cref="IAvailabilityBroadcaster"/> (US-015) — <c>Application</c> nunca
/// pode referenciar <c>Microsoft.AspNetCore.SignalR</c> (ADR-039).
///
/// Chamada SEMPRE de dentro do handler, antes deste retornar (mesmo padrão síncrono de
/// <see cref="IAvailabilityBroadcaster"/> — ver <c>MarkProductUnavailableCommandHandler</c> para o
/// precedente exato que este broadcaster segue).
///
/// [DECISÃO DE ESCOPO] Esta história (US-024) documenta que o consumo reage a
/// <c>order.placed</c>/<c>order.item.fired</c>/<c>order.item.ready</c>/<c>order.item.served</c>/
/// <c>order.item.cancelled</c> — eventos emitidos por um fluxo de KDS/roteamento à praça que ainda
/// não existe nesta solution (fora de E-02). <see cref="ItemAdded"/> é chamado pelo lançamento
/// mínimo de item (<c>AddOrderItemCommand</c>/<c>RepeatOrderItemCommand</c>, gap de US-030) e
/// <see cref="ItemStatusChanged"/> por <c>AdvanceOrderItemStatusCommand</c> — um comando interno
/// mínimo que avança o status de um item (Queued→Fired→...→Served), suficiente para provar a
/// entrega em tempo real de ponta a ponta (mesmo espírito do teste de US-015: "não dá para testar
/// os 2 segundos de ponta a ponta de forma determinística, mas dá para provar que o broadcast é
/// chamado de forma síncrona dentro do handler"). O fluxo completo de KDS/roteamento por praça
/// continua fora do escopo desta história.
/// </summary>
public interface IOrderConsumptionBroadcaster
{
    /// <summary>Item lançado (adicionado ou repetido) na sessão — broadcast <c>order.item.added</c> (EVT-003).</summary>
    Task ItemAdded(
        Guid tenantId,
        Guid tableId,
        Guid orderItemId,
        string productName,
        Guid? repeatedFromItemId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Mudança de status de um item já lançado — tipo do evento é <c>order.item.{status}</c> em
    /// minúsculas com underscore (ex.: <c>order.item.ready</c>, <c>order.item.served</c>), mesmo
    /// formato do contrato de API da US-024 (§7).
    /// </summary>
    Task ItemStatusChanged(
        Guid tenantId,
        Guid tableId,
        Guid orderItemId,
        string productName,
        string status,
        CancellationToken cancellationToken);
}
