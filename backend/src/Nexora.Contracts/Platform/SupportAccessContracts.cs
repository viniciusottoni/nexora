namespace Nexora.Contracts.Platform;

/// <summary>
/// Corpo de <c>POST /v1/platform/tenants/{id}/support-access</c> (US-145 §7) — motivo e duração
/// são sempre exigidos, nunca acesso de suporte silencioso (US-145 §3.2, "fora desta história:
/// acesso de emergência sem registro").
/// </summary>
public sealed record GrantSupportAccessRequest(string Reason, int DurationMinutes);

/// <summary>
/// Resposta de <c>POST /v1/platform/tenants/{id}/support-access</c> (US-145 §7) — <see cref="Token"/>
/// só existe nesta resposta (o raw nunca é persistido, só o hash — mesma convenção de
/// <c>ProvisionTenantResponse.InstallToken</c>/<c>OwnerInvite</c>).
/// </summary>
public sealed record GrantSupportAccessResponse(string Token, DateTimeOffset ExpiresAt, bool NotifiedCustomer);

/// <summary>
/// Uma linha da trilha de acesso de suporte — usada tanto pelo histórico visível ao cliente
/// (<c>GET /v1/tenant/support-access-history</c>) quanto pelo relatório de plataforma
/// (<c>GET /v1/platform/support-access</c>). <see cref="IsActive"/> é calculado no momento da
/// resposta (não persistido) — reflete <c>SupportAccess.IsActive(now)</c>.
/// </summary>
public sealed record SupportAccessSummaryResponse(
    Guid Id,
    Guid TenantId,
    string? TenantName,
    Guid? GrantedTo,
    string Reason,
    int DurationMinutes,
    DateTimeOffset GrantedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    Guid? RevokedBy,
    DateTimeOffset? LastUsedAt,
    bool IsActive);

public sealed record SupportAccessListResponse(IReadOnlyList<SupportAccessSummaryResponse> Data);
