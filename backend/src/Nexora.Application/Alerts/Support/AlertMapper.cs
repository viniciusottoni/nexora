using Nexora.Contracts.Alerts;
using Nexora.Domain.Metrics;

namespace Nexora.Application.Alerts.Support;

public static class AlertMapper
{
    public static AlertResponse ToResponse(Alert alert) => new(
        alert.Id,
        alert.Type,
        alert.Severity.ToString().ToUpperInvariant(),
        alert.EntityType,
        alert.EntityId,
        alert.Message,
        alert.RaisedAt,
        alert.AcknowledgedAt,
        alert.AcknowledgedBy,
        alert.ResolvedAt,
        alert.TargetRoles,
        alert.TargetUserId,
        alert.GroupKey);
}
