using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Commands.TransferTenantOwnership;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — porta de
/// <c>POST /v1/platform/tenants/{id}/ownership-transfers</c>. <see cref="NewOwnerUserId"/> precisa
/// pertencer ao MESMO tenant (RN-015) — usuário de outro tenant vira 404
/// (<c>OWNERSHIP_TARGET_USER_NOT_FOUND</c>), nunca revela se o id existe alhures.
/// </summary>
public sealed record TransferTenantOwnershipCommand(
    Guid TenantId,
    Guid NewOwnerUserId,
    string Reason,
    bool KeepPreviousAsAdmin,
    Guid? ActorId) : ICommand<TransferTenantOwnershipResponse>;
