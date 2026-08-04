using Nexora.Application.Abstractions.Realtime;

namespace Nexora.Infrastructure.Realtime;

/// <summary>
/// Implementação no-op de <see cref="IAlertsBroadcaster"/> para hosts sem hub SignalR de alertas
/// (Nexora.Api.Cloud não tem controller que dispare CallWaiter/RequestBill/AcknowledgeWaiterCall —
/// só o edge marca mesa/comanda, ADR-039 exige que o handler ainda assim resolva a porta). Mesmo
/// padrão de <c>NullBootstrapCatalogImporter</c>. Troque por uma implementação real assim que este
/// host precisar propagar alertas de verdade.
/// </summary>
public sealed class NullAlertsBroadcaster : IAlertsBroadcaster
{
    public Task WaiterCalled(Guid tenantId, Guid tableId, string tableLabel, Guid? waiterId, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task BillRequested(
        Guid tenantId, Guid tableId, string tableLabel, string splitMode, short? people, Guid? waiterId, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task BillRequestCancelled(Guid tenantId, Guid tableId, string tableLabel, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task WaiterCallEscalated(Guid tenantId, Guid tableId, string tableLabel, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task AlertRaised(Domain.Metrics.Alert alert, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task AlertGroupUpdated(Domain.Metrics.Alert alert, int groupCount, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task AlertResolved(Domain.Metrics.Alert alert, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
