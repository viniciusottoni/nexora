namespace Nexora.Application.Abstractions.Realtime;

/// <summary>
/// Porta de propagação em tempo real do estado de conectividade edge↔nuvem (US-034 §7, mensagem
/// WebSocket <c>sync.status</c>) — TODOS os dispositivos da loja precisam saber, sem precisar
/// consultar <c>GET /v1/health</c> em polling, quando a internet caiu ou voltou (cenário Gherkin
/// "Detecção de perda de conexão": "os dispositivos devem exibir o indicador em até 30 segundos").
///
/// Sala emitida: <c>tenant:{id}</c> — no edge, "uma loja = um tenant" (ADR-004), então "todos os
/// dispositivos da loja" (US-034 §7) é exatamente o grupo de tenant já usado por
/// <c>CatalogAvailabilityHub</c>/<c>TableMapHub</c> (ADR-011, "a inscrição é derivada dos claims do
/// token"), nunca escolhida pelo cliente.
///
/// Implementada com SignalR em <c>Nexora.Api.Edge</c> (<c>Realtime.SignalRSyncStatusBroadcaster</c> +
/// <c>Hubs.SyncStatusHub</c>) — <c>Application</c> nunca referencia SignalR (ADR-039).
/// <c>Nexora.Api.Cloud</c> registra um no-op (<c>NullSyncStatusBroadcaster</c>): detectar a queda de
/// internet É um conceito exclusivo do edge (é ele que fala com a nuvem, nunca o inverso) — a nuvem
/// só resolve a porta porque <c>PollSyncHealthCommandHandler</c> pertence ao assembly
/// <c>Application</c> compartilhado (mesmo raciocínio de <see cref="IStationBroadcaster"/> — ver
/// <c>NullStationBroadcaster</c>/<c>NullSyncHealthPoller</c>).
///
/// Chamada SEMPRE de dentro do <c>Handle</c> de <c>PollSyncHealthCommand</c>, na mesma execução que
/// grava <c>EdgeInstallation.RecordHeartbeat</c>/<c>DomainEvent</c> — mesmo padrão síncrono de
/// <see cref="IStationBroadcaster"/>/<see cref="IAvailabilityBroadcaster"/>.
/// </summary>
public interface ISyncStatusBroadcaster
{
    /// <summary>
    /// EVT-083/EVT-084 (US-034 §6/§7) — chamado só quando <c>EdgeInstallation.RecordHeartbeat</c>
    /// detecta uma TRANSIÇÃO real de conectividade (nunca a cada poll, ver
    /// <c>SyncConnectivityTransition</c>). Contrato exato da mensagem (US-034 §7):
    /// <c>{ "type": "sync.status", "data": { "online": bool, "pendingEvents": N, "lastSyncAt": "..." } }</c>.
    /// </summary>
    Task SyncStatusChanged(
        Guid tenantId,
        bool online,
        int pendingEvents,
        DateTimeOffset? lastSyncAt,
        CancellationToken cancellationToken);
}
