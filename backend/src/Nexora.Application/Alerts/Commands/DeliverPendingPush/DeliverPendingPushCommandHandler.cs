using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Domain.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Commands.DeliverPendingPush;

internal sealed class DeliverPendingPushCommandHandler : IRequestHandler<DeliverPendingPushCommand, Result<int>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPushNotificationSender _pushSender;

    public DeliverPendingPushCommandHandler(IApplicationDbContext db, IPushNotificationSender pushSender)
    {
        _db = db;
        _pushSender = pushSender;
    }

    public async Task<Result<int>> Handle(DeliverPendingPushCommand request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId;
        await _db.SetTenantContextAsync(tenantId, cancellationToken);

        var pending = await _db.Alerts
            .Where(a => a.TenantId == tenantId && a.ResolvedAt == null && a.PushedAt == null
                        && (a.Severity == AlertSeverity.High || a.Severity == AlertSeverity.Critical))
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return Result<int>.Success(0);
        }

        var userRoles = await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.TenantId == tenantId)
            .Select(ur => new UserRoleRow(ur.UserId, ur.Role.Code))
            .ToListAsync(cancellationToken);

        var subscriptions = await _db.PushSubscriptions.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var subscriptionsByUser = subscriptions.GroupBy(s => s.UserId).ToDictionary(g => g.Key, g => g.ToList());

        var sentCount = 0;
        foreach (var alert in pending)
        {
            var userIds = ResolveTargetUserIds(alert, userRoles);

            foreach (var userId in userIds)
            {
                if (!subscriptionsByUser.TryGetValue(userId, out var userSubscriptions))
                {
                    continue;
                }

                foreach (var subscription in userSubscriptions)
                {
                    await _pushSender.SendAsync(
                        new PushTarget(subscription.Endpoint, subscription.P256dhKey, subscription.AuthKey),
                        new PushPayload(
                            Title: AlertTitle(alert.Severity),
                            Body: alert.Message,
                            Severity: alert.Severity.ToString().ToUpperInvariant(),
                            AlertId: alert.Id),
                        cancellationToken);
                    sentCount++;
                }
            }

            alert.MarkPushed();
        }

        return Result<int>.Success(sentCount);
    }

    private static IReadOnlyCollection<Guid> ResolveTargetUserIds(Alert alert, IReadOnlyList<UserRoleRow> userRoles)
    {
        if (alert.TargetUserId is { } userId)
        {
            return new[] { userId };
        }

        var roles = new HashSet<string>(alert.TargetRoles.Select(r => r.ToUpperInvariant()), StringComparer.Ordinal);
        return userRoles
            .Where(ur => roles.Contains(ur.RoleCode.ToUpperInvariant()))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToList();
    }

    private static string AlertTitle(AlertSeverity severity) => severity switch
    {
        AlertSeverity.Critical => "Alerta crítico",
        AlertSeverity.High => "Alerta importante",
        _ => "Alerta",
    };

    private sealed record UserRoleRow(Guid UserId, string RoleCode);
}
