namespace Nexora.Contracts.Alerts;

/// <summary>US-080 §7 <c>GET /v1/alerts</c> — um alerta individual (linha de <c>alert</c>).</summary>
public sealed record AlertResponse(
    Guid Id,
    string Type,
    string Severity,
    string? EntityType,
    Guid? EntityId,
    string Message,
    DateTimeOffset RaisedAt,
    DateTimeOffset? AcknowledgedAt,
    Guid? AcknowledgedBy,
    DateTimeOffset? ResolvedAt,
    IReadOnlyList<string> TargetRoles,
    Guid? TargetUserId,
    string? GroupKey);

public sealed record AlertListResponse(IReadOnlyList<AlertResponse> Alerts, string? NextCursor);

/// <summary>US-083 §7 <c>GET /v1/alerts?grouped=true</c> — um grupo de alertas repetidos do mesmo tipo/janela.</summary>
public sealed record AlertGroupResponse(
    string Type,
    int Count,
    string Severity,
    string Message,
    DateTimeOffset FirstRaisedAt,
    DateTimeOffset LastRaisedAt,
    IReadOnlyList<AlertResponse> Alerts);

public sealed record AlertGroupListResponse(IReadOnlyList<AlertGroupResponse> Groups);

/// <summary>US-080 §7 <c>GET/PATCH /v1/tenant/thresholds</c> — limiar monetário como string (ADR-017).</summary>
public sealed record TenantThresholdsResponse(
    int OrderWarnMinutes,
    int OrderCriticalMinutes,
    int ItemInWindowMinutes,
    int TableIdleMinutes,
    string CashDivergenceAlert,
    decimal CmvDivergencePercent,
    int SyncDelayMinutes,
    int DineInPromiseMinutes,
    int DeliveryPromiseMinutes,
    decimal AvgTimeAboveTargetPercent,
    int CancellationCountThreshold,
    int CancellationWindowMinutes,
    decimal DiscountAboveThresholdPercent,
    int DiscountWindowMinutes);

/// <summary>Corpo do PATCH — todo campo é opcional (merge parcial sobre o valor atual).</summary>
public sealed record UpdateTenantThresholdsRequest(
    int? OrderWarnMinutes,
    int? OrderCriticalMinutes,
    int? ItemInWindowMinutes,
    int? TableIdleMinutes,
    string? CashDivergenceAlert,
    decimal? CmvDivergencePercent,
    int? SyncDelayMinutes,
    int? DineInPromiseMinutes,
    int? DeliveryPromiseMinutes,
    decimal? AvgTimeAboveTargetPercent,
    int? CancellationCountThreshold,
    int? CancellationWindowMinutes,
    decimal? DiscountAboveThresholdPercent,
    int? DiscountWindowMinutes);

/// <summary>US-082 §7 — uma entrada da matriz de direcionamento, já totalmente resolvida (override ou padrão).</summary>
public sealed record AlertRoutingRuleResponse(IReadOnlyList<string> Roles, string Scope, int? EscalateAfterSeconds, int? GroupWindowSeconds);

/// <summary>Corpo do PATCH por tipo — campos nulos preservam o valor atual/padrão daquele tipo.</summary>
public sealed record AlertRoutingRulePatch(IReadOnlyList<string>? Roles, string? Scope, int? EscalateAfterSeconds, int? GroupWindowSeconds);

/// <summary>US-081 §7 <c>POST /v1/notifications/subscribe</c> — Web Push (VAPID, RFC 8291/8292).</summary>
public sealed record SubscribePushRequest(string Endpoint, PushKeysRequest Keys);

public sealed record PushKeysRequest(string P256dh, string Auth);

public sealed record SubscribePushResponse(bool Subscribed);
