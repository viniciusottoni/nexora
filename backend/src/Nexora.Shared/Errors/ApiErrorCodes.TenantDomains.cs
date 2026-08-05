namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do módulo de domínio próprio por tenant (US-143, ADR-021).
/// </summary>
/// <remarks>
/// Por instrução explícita desta tarefa, <c>ResultExtensions.MapErrorCode</c> (Api.Edge e
/// Api.Cloud) NÃO foi editado aqui — os quatro códigos abaixo caem hoje no <c>default</c> daquele
/// switch (500 Internal Server Error) até alguém adicionar as entradas. Mapeamento HTTP pretendido,
/// para quem for adicionar:
/// <list type="table">
/// <item><term><see cref="TenantDomainAlreadyRegistered"/></term><description>422 Unprocessable Entity, recoverable=true, requiresAuthorization=false — o cliente pode tentar outro domínio.</description></item>
/// <item><term><see cref="TenantDomainVerificationFailed"/></term><description>422 Unprocessable Entity, recoverable=true, requiresAuthorization=false — instrução do que falta fica em <c>Error</c>/<c>Detail</c> (US-143 §4, "instruções claras do que fazer").</description></item>
/// <item><term><see cref="TenantDomainNotFound"/></term><description>404 Not Found, recoverable=false, requiresAuthorization=false — mesma convenção de <c>TenantNotFound</c>.</description></item>
/// <item><term><see cref="TenantDomainCertificateIssuanceFailed"/></term><description>503 Service Unavailable, recoverable=true, requiresAuthorization=false — falha de dependência externa (emissor de certificado), mesma família de <c>BrandingStorageUnavailable</c>.</description></item>
/// </list>
/// </remarks>
public static partial class ApiErrorCodes
{
    /// <summary>RN-015: domínio já registrado (por este tenant ou por outro) — índice único global <c>uq_tenant_domain_domain</c>.</summary>
    public const string TenantDomainAlreadyRegistered = "TENANT_DOMAIN_ALREADY_REGISTERED";

    /// <summary>Registro TXT esperado não encontrado (ou com valor divergente) no DNS do domínio.</summary>
    public const string TenantDomainVerificationFailed = "TENANT_DOMAIN_VERIFICATION_FAILED";

    /// <summary>Nenhum domínio com o id informado (em nenhum tenant).</summary>
    public const string TenantDomainNotFound = "TENANT_DOMAIN_NOT_FOUND";

    /// <summary>Emissão/renovação de certificado TLS falhou (<c>ICertificateIssuer</c>).</summary>
    public const string TenantDomainCertificateIssuanceFailed = "TENANT_DOMAIN_CERTIFICATE_ISSUANCE_FAILED";
}
