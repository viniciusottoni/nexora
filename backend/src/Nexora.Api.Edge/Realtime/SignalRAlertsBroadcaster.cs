using Nexora.Api.Edge.Hubs;
using Nexora.Application.Abstractions.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Nexora.Api.Edge.Realtime;

/// <summary>
/// Implementação de <see cref="IAlertsBroadcaster"/> sobre <see cref="IHubContext{THub}"/> (US-025/
/// US-026) — mesma mensagem <c>{ type, data }</c> pelo método de cliente <c>alert</c> (mesmo formato
/// de <c>productAvailabilityChanged</c>/<c>tableConsumptionChanged</c>), só o NOME do método e das
/// salas mudam (ver docstring de <see cref="AlertsHub"/>).
/// </summary>
internal sealed class SignalRAlertsBroadcaster : IAlertsBroadcaster
{
    private const string MethodName = "alert";

    private readonly IHubContext<AlertsHub> _hub;

    public SignalRAlertsBroadcaster(IHubContext<AlertsHub> hub)
    {
        _hub = hub;
    }

    public Task WaiterCalled(Guid tenantId, Guid tableId, string tableLabel, Guid? waiterId, CancellationToken cancellationToken)
    {
        var group = waiterId is { } id ? AlertsHub.UserGroup(id) : AlertsHub.RoleGroup("waiter");
        return _hub.Clients.Group(group).SendAsync(
            MethodName,
            new { type = "table.waiter_called", data = new { tableId, label = tableLabel } },
            cancellationToken);
    }

    public Task BillRequested(
        Guid tenantId, Guid tableId, string tableLabel, string splitMode, short? people, Guid? waiterId, CancellationToken cancellationToken)
    {
        var groups = new List<string> { AlertsHub.RoleGroup("cashier") };
        if (waiterId is { } id)
        {
            groups.Add(AlertsHub.UserGroup(id));
        }

        return _hub.Clients.Groups(groups).SendAsync(
            MethodName,
            new { type = "table.bill_requested", data = new { tableId, label = tableLabel, splitMode, people } },
            cancellationToken);
    }

    public Task BillRequestCancelled(Guid tenantId, Guid tableId, string tableLabel, CancellationToken cancellationToken) =>
        _hub.Clients.Group(AlertsHub.RoleGroup("cashier")).SendAsync(
            MethodName,
            new { type = "table.bill_request_cancelled", data = new { tableId, label = tableLabel } },
            cancellationToken);

    public Task WaiterCallEscalated(Guid tenantId, Guid tableId, string tableLabel, CancellationToken cancellationToken) =>
        _hub.Clients.Group(AlertsHub.RoleGroup("waiter")).SendAsync(
            MethodName,
            new { type = "table.waiter_call_escalated", data = new { tableId, label = tableLabel } },
            cancellationToken);

    public Task AlertRaised(Domain.Metrics.Alert alert, CancellationToken cancellationToken) =>
        _hub.Clients.Groups(TargetGroups(alert)).SendAsync(MethodName, BuildEnvelope("alert.raised", alert, count: null), cancellationToken);

    public Task AlertGroupUpdated(Domain.Metrics.Alert alert, int groupCount, CancellationToken cancellationToken) =>
        _hub.Clients.Groups(TargetGroups(alert)).SendAsync(MethodName, BuildEnvelope("alert.group_updated", alert, groupCount), cancellationToken);

    public Task AlertResolved(Domain.Metrics.Alert alert, CancellationToken cancellationToken) =>
        _hub.Clients.Groups(TargetGroups(alert)).SendAsync(MethodName, BuildEnvelope("alert.resolved", alert, count: null), cancellationToken);

    /// <summary>
    /// US-081 §7 <c>{ type, data: { alertId, alertType, severity, entityId, message } }</c> — as
    /// mesmas salas do resto deste broadcaster: <c>role:{papel}</c> por papel-alvo E/OU
    /// <c>user:{id}</c> quando o alerta tem destinatário único (US-082, escopo RESPONSIBLE/TABLE_OWNER).
    /// </summary>
    private static List<string> TargetGroups(Domain.Metrics.Alert alert)
    {
        var groups = alert.TargetRoles.Select(r => AlertsHub.RoleGroup(r)).ToList();
        if (alert.TargetUserId is { } userId)
        {
            groups.Add(AlertsHub.UserGroup(userId));
        }

        return groups;
    }

    private static object BuildEnvelope(string type, Domain.Metrics.Alert alert, int? count) => new
    {
        type,
        data = new
        {
            alertId = alert.Id,
            alertType = alert.Type,
            severity = alert.Severity.ToString().ToUpperInvariant(),
            entityType = alert.EntityType,
            entityId = alert.EntityId,
            message = alert.Message,
            groupKey = alert.GroupKey,
            count
        }
    };
}
