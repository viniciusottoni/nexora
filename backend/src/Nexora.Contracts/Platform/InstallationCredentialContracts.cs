namespace Nexora.Contracts.Platform;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — portas de
/// <c>POST /v1/platform/installations/{installationId}/tokens</c> e
/// <c>DELETE /v1/platform/installations/{installationId}/tokens/{credentialId}</c>.
/// </summary>
public sealed record ReissueInstallationTokenRequest(string Reason, int ExpiresInHours);

/// <summary>
/// <see cref="InstallToken"/>/<see cref="InstallCommand"/> só aparecem NESTA resposta — exibição
/// única (mesmo padrão do <c>installToken</c>/<c>installCommand</c> do
/// <c>POST /v1/platform/tenants</c> original, ver <c>ProvisionTenantResponse</c>). A action
/// <c>ReissueToken</c> é marcada com <c>IdempotencyRedactFieldsAttribute("installToken", "installCommand")</c>
/// — a resposta ARMAZENADA para reenvio idempotente (ADR-020, 24h) tem esses dois campos
/// substituídos por <c>null</c> antes de ser gravada; a resposta AO VIVO da primeira chamada (a
/// única que este record representa de fato) sempre os traz preenchidos.
/// </summary>
public sealed record ReissueInstallationTokenResponse(
    Guid CredentialId,
    DateTimeOffset ExpiresAt,
    string InstallToken,
    string InstallCommand);

public sealed record RevokeInstallationCredentialRequest(string Reason);
