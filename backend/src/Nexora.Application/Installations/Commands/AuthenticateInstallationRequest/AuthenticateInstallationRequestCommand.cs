using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Installations.Commands.AuthenticateInstallationRequest;

/// <summary>
/// Porta de <c>installation-auth.guard.ts</c> — protocolo de assinatura Ed25519 usado pelas
/// rotas <c>@InstallationAuthenticated()</c> (hoje: GET /v1/sync/pull e /v1/sync/health).
/// Diferente do guard original (que resolvia tudo dentro do próprio filtro HTTP com SQL bruto),
/// aqui a verificação vira um Command normal porque precisa de acesso transacional ao banco
/// (consumo do nonce anti-replay) — Api.Cloud só pode chegar ao banco via <c>ISender</c>
/// (regra de fronteira do ADR-037: controller nunca injeta AppDbContext).
/// </summary>
public sealed record AuthenticateInstallationRequestCommand(
    Guid InstallationId,
    string Timestamp,
    string Nonce,
    string Signature,
    string HttpMethod,
    string RequestPath) : ICommand<InstallationAuthContext>;

/// <summary>Contexto de tenant/loja resolvido a partir da instalação autenticada.</summary>
public sealed record InstallationAuthContext(Guid TenantId, Guid StoreId, Guid InstallationId);
