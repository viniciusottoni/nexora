namespace Nexora.Contracts.Tenants;

/// <summary>Corpo de <c>POST /v1/auth/invitations/accept</c> — espelha <c>AcceptOwnerInviteDto</c> do NestJS original.</summary>
public sealed record AcceptOwnerInviteRequest(string Token, string Password);
