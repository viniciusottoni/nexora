using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Tenants.Commands.RevokeOwnerInvite;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — porta de
/// <c>DELETE /v1/platform/tenants/{id}/owner-invites/{inviteId}</c>. RN-015 (isolamento total):
/// <see cref="InviteId"/> só é resolvido se pertencer a <see cref="TenantId"/> — outro tenant vira
/// 404, nunca 403 (ADR-021).
/// </summary>
public sealed record RevokeOwnerInviteCommand(
    Guid TenantId,
    Guid InviteId,
    string Reason,
    Guid? ActorId) : ICommand;
