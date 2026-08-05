using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Commands.TransitionTenantStatus;

/// <summary>
/// US-153 · Ciclo de vida do estabelecimento — transição administrativa de status
/// (<c>POST /v1/platform/tenants/{id}/status-transitions</c>), exclusiva da policy
/// <c>PlatformAdmin</c> (RN-015 "isolamento total": afeta só o tenant informado).
/// </summary>
/// <param name="ExpectedVersion">
/// Extraído do header <c>If-Match</c> pelo controller (aspas removidas, <c>int.TryParse</c>) — <c>0</c>
/// quando ausente/malformado, valor que nunca combina com <see cref="Nexora.Domain.Platform.Tenant.StatusVersion"/>
/// real (começa em 1), garantindo 409 CONCURRENCY_CONFLICT em vez de aceitar a transição às cegas.
/// </param>
/// <param name="ActorId">Claim <c>sub</c> do administrador de plataforma (<c>ICurrentTenantContext.UserId</c>) — RN-004 "ator".</param>
public sealed record TransitionTenantStatusCommand(
    Guid TenantId,
    string TargetStatus,
    string Reason,
    DateTimeOffset? EffectiveAt,
    int ExpectedVersion,
    Guid? ActorId) : ICommand<TenantStatusTransitionResponse>;
