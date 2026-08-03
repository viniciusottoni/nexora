using Nexora.Application.Abstractions.Realtime;

namespace Nexora.Infrastructure.Realtime;

/// <summary>
/// Implementação no-op de <see cref="IStationBroadcaster"/> para hosts sem hub SignalR de KDS
/// (Nexora.Api.Cloud não tem praça/KDS local — só o edge, ADR-039 exige que o handler ainda assim
/// resolva a porta). Mesmo padrão de <see cref="NullOrderConsumptionBroadcaster"/> (US-024).
/// </summary>
public sealed class NullStationBroadcaster : IStationBroadcaster
{
    public Task OrderPlaced(
        Guid tenantId,
        Guid orderId,
        string shortCode,
        Guid? tableId,
        string? tableLabel,
        string channel,
        IReadOnlyList<StationBroadcastItem> items,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ItemQueued(
        Guid tenantId,
        Guid orderId,
        string shortCode,
        Guid? tableId,
        string? tableLabel,
        string channel,
        StationBroadcastItem item,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ItemStatusChanged(
        Guid tenantId,
        Guid stationId,
        Guid orderItemId,
        string productName,
        string status,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
