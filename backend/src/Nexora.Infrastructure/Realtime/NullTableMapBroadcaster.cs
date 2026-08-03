using Nexora.Application.Abstractions.Realtime;
using Nexora.Contracts.Tables;

namespace Nexora.Infrastructure.Realtime;

/// <summary>
/// Implementação no-op de <see cref="ITableMapBroadcaster"/> para hosts sem hub SignalR de mapa de
/// mesas (Nexora.Api.Cloud não gerencia mesa/comanda em tempo real — só o edge, ADR-039 exige que o
/// handler ainda assim resolva a porta). Mesmo padrão de <c>NullBootstrapCatalogImporter</c>.
/// </summary>
public sealed class NullTableMapBroadcaster : ITableMapBroadcaster
{
    public Task NotifyTableChangedAsync(Guid tenantId, TableMapEntryResponse table, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task NotifySignalAsync(Guid tenantId, string type, object data, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
