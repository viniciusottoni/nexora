namespace Nexora.Contracts.Tenants;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — contratos de
/// <c>GET/POST/DELETE /v1/platform/tenants/{id}/ownership...</c>. Espelha
/// <c>packages/contracts/src/tenant-ownership.ts</c> (zod) — as duas listas precisam continuar em
/// sincronia, mesma convenção de <see cref="TenantPlanContracts"/>/<see cref="TenantOverviewContracts"/>.
/// Nenhum destes contratos carrega segredo (token bruto ou hash) — ver
/// <c>OwnershipSecretNeverLeaksTests</c> (Nexora.UnitTests).
/// </summary>
/// <summary><see cref="Status"/>: <c>"NONE"</c> | <c>"INVITED"</c> | <c>"ACTIVE"</c> | <c>"INACTIVE"</c> | <c>"BLOCKED"</c>. <see cref="Id"/>/<see cref="Name"/>/<see cref="Email"/> nulos quando <see cref="Status"/> é <c>"NONE"</c> (sem proprietário resolvido).</summary>
public sealed record TenantOwnershipOwnerResponse(Guid? Id, string? Name, string? Email, string Status);

/// <summary>
/// Uma linha do histórico de convites. <see cref="Status"/>: <c>"PENDING"</c> | <c>"ACCEPTED"</c> |
/// <c>"EXPIRED"</c> | <c>"REVOKED"</c>. <see cref="DeliveryStatus"/> (do <c>email_outbox</c>
/// correlacionado): <c>"PENDING"</c> | <c>"SENT"</c> | <c>"FAILED"</c> | <c>"UNKNOWN"</c> (convite
/// emitido antes desta US, sem correlação). Nunca carrega <see cref="Nexora.Domain.Platform.OwnerInvite.SecretHash"/>
/// nem o token bruto (que nunca é persistido em lugar nenhum, ver <c>ProvisionTenantCommandHandler</c>).
/// </summary>
public sealed record TenantOwnershipInviteResponse(
    Guid Id,
    string SentTo,
    string Status,
    string DeliveryStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConsumedAt,
    DateTimeOffset? RevokedAt,
    string? RevokedReason,
    string? Reason);

public sealed record TenantOwnershipTransferHistoryResponse(
    Guid Id,
    Guid PreviousOwnerUserId,
    Guid NewOwnerUserId,
    string Reason,
    bool PreviousKeptAsAdmin,
    DateTimeOffset TransferredAt);

public sealed record TenantOwnershipResponse(
    TenantOwnershipOwnerResponse Owner,
    IReadOnlyList<TenantOwnershipInviteResponse> Invites,
    IReadOnlyList<TenantOwnershipTransferHistoryResponse> Transfers);

/// <summary>
/// Corpo de <c>POST /v1/platform/tenants/{id}/owner-invites</c> — cobre TANTO reenvio (e-mail/nome
/// iguais ao atual) QUANTO correção (e-mail/nome diferentes), mesma decisão de unificar num único
/// comando/endpoint documentada no relatório final da tarefa: o contrato abreviado da US (§ Contrato
/// de API) já define UM ÚNICO endpoint para os dois cenários Gherkin ("Convite expirado" e "E-mail
/// corrigido").
/// </summary>
public sealed record CreateOwnerInviteRequest(string Name, string Email, string Reason);

public sealed record CreateOwnerInviteResponse(Guid InviteId, string SentTo, DateTimeOffset ExpiresAt);

/// <summary>Corpo de <c>DELETE /v1/platform/tenants/{id}/owner-invites/{inviteId}</c>.</summary>
public sealed record RevokeOwnerInviteRequest(string Reason);

/// <summary>Corpo de <c>POST /v1/platform/tenants/{id}/ownership-transfers</c>.</summary>
public sealed record TransferTenantOwnershipRequest(Guid NewOwnerUserId, string Reason, bool KeepPreviousAsAdmin);

public sealed record TransferTenantOwnershipResponse(
    Guid PreviousOwnerUserId,
    Guid NewOwnerUserId,
    bool PreviousKeptAsAdmin,
    DateTimeOffset TransferredAt);

/// <summary>Corpo de <c>POST /v1/platform/tenants/{id}/ownership/unlock</c>.</summary>
public sealed record UnlockOwnerAccessRequest(string Reason);

public sealed record UnlockOwnerAccessResponse(Guid UserId, string Status);
