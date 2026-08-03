using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Installation.Abstractions;
using Nexora.Domain.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Installation.Commands.PollSyncHealth;

/// <summary>
/// US-034 §6/§7: além de gravar o resultado do ping (comportamento original), agora COMPARA o
/// estado de conectividade anterior com o observado neste poll e, quando há uma transição real
/// (Domain decide a regra — ver <see cref="EdgeInstallation.RecordHeartbeat"/>), grava o
/// <c>DomainEvent</c> correspondente (EVT-083 <c>edge.offline_detected</c>/EVT-084
/// <c>edge.reconnected</c>) e propaga <c>sync.status</c> a todos os dispositivos da loja via
/// <see cref="ISyncStatusBroadcaster"/> — tudo na MESMA transação/execução deste handler
/// (<c>TransactionBehavior</c> cobre o <c>SaveChangesAsync</c>, ADR-006/007).
/// </summary>
internal sealed class PollSyncHealthCommandHandler : IRequestHandler<PollSyncHealthCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ISyncHealthPoller _poller;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ISyncStatusBroadcaster _syncStatusBroadcaster;
    private readonly ILogger<PollSyncHealthCommandHandler> _logger;

    public PollSyncHealthCommandHandler(
        IApplicationDbContext db,
        ISyncHealthPoller poller,
        IEventOriginProvider eventOrigin,
        ISyncStatusBroadcaster syncStatusBroadcaster,
        ILogger<PollSyncHealthCommandHandler> logger)
    {
        _db = db;
        _poller = poller;
        _eventOrigin = eventOrigin;
        _syncStatusBroadcaster = syncStatusBroadcaster;
        _logger = logger;
    }

    public async Task<Result> Handle(PollSyncHealthCommand request, CancellationToken cancellationToken)
    {
        var installation = await _db.EdgeInstallations.FirstOrDefaultAsync(cancellationToken);
        if (installation is null)
        {
            // Edge ainda não passou pelo bootstrap (ImportBootstrapCommand) — nada a fazer.
            _logger.LogWarning("sync.health.skip: instalação edge ainda não importada.");
            return Result.Success();
        }

        // Capturado ANTES do heartbeat atualizar LastSeenAt — é o último "visto por último" que
        // valia enquanto a conectividade ainda era a anterior, exatamente o que EVT-083 espera no
        // campo lastSyncAt ("desde quando os dados estão defasados").
        var previousLastSeenAt = installation.LastSeenAt;

        var poll = await _poller.PollAsync(cancellationToken);

        var healthJson = $$"""{"sync":"{{poll.Status}}","httpStatus":{{(poll.HttpStatusCode is { } code ? code : "null")}},"checkedAt":"{{DateTimeOffset.UtcNow:O}}"}""";

        var observedConnectivity = MapConnectivity(poll.Status);

        var transition = installation.RecordHeartbeat(installation.LastSyncedSeq, clockOffsetMs: null, observedConnectivity, healthJson);

        _logger.LogInformation("sync.health: {Status}", poll.Status);

        if (transition.Kind != SyncTransitionKind.None)
        {
            await EmitTransitionAsync(installation, transition, previousLastSeenAt, cancellationToken);
        }

        // SaveChangesAsync é feito pelo TransactionBehavior (commands).
        return Result.Success();
    }

    private async Task EmitTransitionAsync(
        EdgeInstallation installation,
        SyncConnectivityTransition transition,
        DateTimeOffset? previousLastSeenAt,
        CancellationToken cancellationToken)
    {
        // Nenhum evento do outbox se perde ou é tocado aqui — só CONTAMOS o que já está
        // acumulado (ADR-007 continua sendo o único dono da escrita/leitura do outbox).
        var pendingEvents = await _db.OutboxEntries.AsNoTracking()
            .CountAsync(o => o.Status != "SYNCED", cancellationToken);

        var occurredAt = DateTimeOffset.UtcNow;

        if (transition.Kind == SyncTransitionKind.WentOffline)
        {
            // EVT-083 edge.offline_detected (US-034 §6) — payload: lastSyncAt, pendingEvents.
            _db.DomainEvents.Add(DomainEvent.Create(
                installation.TenantId,
                type: "edge.offline_detected",
                aggregateType: "edge_installation",
                aggregateId: installation.Id,
                payload: JsonSerializer.Serialize(new { lastSyncAt = previousLastSeenAt, pendingEvents }),
                origin: _eventOrigin.Origin,
                occurredAt: occurredAt,
                storeId: installation.StoreId));

            await _syncStatusBroadcaster.SyncStatusChanged(
                installation.TenantId, online: false, pendingEvents, previousLastSeenAt, cancellationToken);
        }
        else
        {
            // EVT-084 edge.reconnected (US-034 §6) — payload: offlineSeconds, pendingEvents.
            var offlineSeconds = (int)Math.Round(transition.OfflineDuration?.TotalSeconds ?? 0);

            _db.DomainEvents.Add(DomainEvent.Create(
                installation.TenantId,
                type: "edge.reconnected",
                aggregateType: "edge_installation",
                aggregateId: installation.Id,
                payload: JsonSerializer.Serialize(new { offlineSeconds, pendingEvents }),
                origin: _eventOrigin.Origin,
                occurredAt: occurredAt,
                storeId: installation.StoreId));

            await _syncStatusBroadcaster.SyncStatusChanged(
                installation.TenantId, online: true, pendingEvents, occurredAt, cancellationToken);
        }
    }

    private static SyncConnectivity MapConnectivity(DependencyStatus status) => status switch
    {
        DependencyStatus.Ok => SyncConnectivity.Online,
        DependencyStatus.Degraded => SyncConnectivity.Online,
        DependencyStatus.Down => SyncConnectivity.Offline,
        _ => SyncConnectivity.Unknown,
    };
}
