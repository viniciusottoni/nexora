namespace Nexora.Contracts.Installations;

/// <summary>
/// Corpo do POST /v1/platform/installations/register (cloud). O token de uso único vem pelo
/// header <c>X-Install-Token</c> (nunca no corpo — evita acabar em log de request body).
/// </summary>
public sealed record RegisterInstallationRequest(
    Guid InstallationId,
    string Hostname,
    string Version,
    string PublicKey);

public sealed record RegisterInstallationResponse(
    RegisteredTenant Tenant,
    RegisteredStore Store,
    int ConfigVersion,
    string SyncEndpoint,
    string PinLookupPepper);

public sealed record RegisteredTenant(Guid Id, string Name, string Slug);

public sealed record RegisteredStore(Guid Id, string Name, string Timezone);
