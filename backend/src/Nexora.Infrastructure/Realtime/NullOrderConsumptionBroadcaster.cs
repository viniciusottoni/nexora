using Nexora.Application.Abstractions.Realtime;

namespace Nexora.Infrastructure.Realtime;

/// <summary>
/// Implementação no-op de <see cref="IOrderConsumptionBroadcaster"/> para hosts sem hub SignalR de
/// consumo de mesa (Nexora.Api.Cloud não tem controller de pedido/item — só o edge, ADR-039 exige
/// que o handler ainda assim resolva a porta). Mesmo padrão de <c>NullBootstrapCatalogImporter</c>.
/// </summary>
public sealed class NullOrderConsumptionBroadcaster : IOrderConsumptionBroadcaster
{
    public Task ItemAdded(
        Guid tenantId,
        Guid tableId,
        Guid orderItemId,
        string productName,
        Guid? repeatedFromItemId,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ItemStatusChanged(
        Guid tenantId,
        Guid tableId,
        Guid orderItemId,
        string productName,
        string status,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
