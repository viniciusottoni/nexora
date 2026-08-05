namespace Nexora.Contracts.Platform;

/// <summary>Contratos de domínio próprio por cliente (US-143 §7) — vivem em <c>Nexora.Contracts</c> (ADR-039: só referencia <c>Nexora.Domain</c>).</summary>
public sealed record RegisterTenantDomainRequest(string Domain);

/// <summary>Instruções de DNS que o cliente precisa repassar ao provedor (US-143 §10).</summary>
public sealed record TenantDomainVerificationInstructionsResponse(string Type, string Name, string Value);

public sealed record TenantDomainResponse(
    Guid Id,
    Guid TenantId,
    string Domain,
    string Status,
    bool IsPrimary,
    DateTimeOffset? VerifiedAt,
    string CertStatus,
    DateTimeOffset? CertIssuedAt,
    DateTimeOffset? CertExpiresAt,
    DateTimeOffset CreatedAt);

/// <summary>Corpo de <c>201</c> de <c>POST /v1/platform/tenants/{id}/domains</c> — espelha o exemplo do US-143 §7.</summary>
public sealed record RegisterTenantDomainResponse(
    TenantDomainResponse Domain,
    TenantDomainVerificationInstructionsResponse Verification,
    string Status);

public sealed record VerifyTenantDomainResponse(TenantDomainResponse Domain);

public sealed record TenantDomainListResponse(IReadOnlyList<TenantDomainResponse> Data);
