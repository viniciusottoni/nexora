using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Commands.UnlockOwnerAccess;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — porta de
/// <c>POST /v1/platform/tenants/{id}/ownership/unlock</c>. Desbloqueia o <c>AppUser</c> do
/// proprietário SEM jamais definir ou visualizar sua senha — o suporte só reverte
/// <see cref="Nexora.Domain.Platform.AppUser.Status"/> de <c>Blocked</c> para <c>Active</c>
/// (<see cref="Nexora.Domain.Platform.AppUser.Unblock"/>); a próxima autenticação continua exigindo
/// a senha que o próprio proprietário já tinha.
/// </summary>
public sealed record UnlockOwnerAccessCommand(
    Guid TenantId,
    string Reason,
    Guid? ActorId) : ICommand<UnlockOwnerAccessResponse>;
