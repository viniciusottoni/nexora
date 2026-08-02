using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Tenants.Commands.AcceptOwnerInvitation;

/// <summary>
/// Aceita o convite de dono enviado no provisionamento, definindo a senha inicial e ativando
/// o usuário. Porta de <c>owner-invitation-acceptance.ts</c> (rota <c>POST /v1/auth/invitations/accept</c>,
/// pública — o token É a credencial, não há sessão prévia).
/// </summary>
public sealed record AcceptOwnerInvitationCommand(string Token, string Password) : ICommand;
