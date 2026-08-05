using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Commands.ReissueOwnerInvite;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — porta de
/// <c>POST /v1/platform/tenants/{id}/owner-invites</c>. Cobre os dois cenários Gherkin "Convite
/// expirado" (reenvio: <see cref="Name"/>/<see cref="Email"/> iguais ao atual) e "E-mail corrigido
/// antes da aceitação" (correção: <see cref="Email"/> diferente) com o MESMO comando — decisão
/// documentada no relatório final: o contrato abreviado da US só lista um único endpoint para os
/// dois casos, e a lógica de negócio (revogar o convite anterior + emitir um novo com 72h) é
/// idêntica; só o que muda é se <see cref="Nexora.Domain.Platform.AppUser.Email"/> realmente muda.
/// </summary>
public sealed record ReissueOwnerInviteCommand(
    Guid TenantId,
    string Name,
    string Email,
    string Reason,
    Guid? ActorId) : ICommand<CreateOwnerInviteResponse>;
