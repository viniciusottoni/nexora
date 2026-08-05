using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Platform;

/// <summary>
/// Implementação de <see cref="ITenantDomainRedirectResolver"/> usada por
/// <c>TenantDomainRedirectMiddleware</c> (US-143 §3.1). <c>Tenant.Slug + "." + DefaultDomainSuffix</c>
/// é o "domínio padrão" (ex.: <c>donabetinha.plataforma.com.br</c>) — não existe hoje uma coluna
/// que grave isso (é convenção de DNS, não dado do tenant); <c>Tenant.Domain</c>, por outro lado,
/// já é o domínio próprio primário verificado (US-143, espelhado por
/// <c>Tenant.SetCustomDomain</c>). Quando o host da requisição bate com o padrão calculado E o
/// tenant já tem um <c>Tenant.Domain</c> diferente configurado, este método devolve o alvo do
/// redirecionamento.
/// </summary>
public sealed class TenantDomainRedirectResolver : ITenantDomainRedirectResolver
{
    private readonly IApplicationDbContext _db;
    private readonly PlatformDomainOptions _options;

    public TenantDomainRedirectResolver(IApplicationDbContext db, IOptions<PlatformDomainOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<string?> ResolvePrimaryDomainAsync(string normalizedHost, CancellationToken cancellationToken)
    {
        var suffix = _options.DefaultDomainSuffix;
        if (string.IsNullOrWhiteSpace(suffix))
        {
            // Sem "Platform:DefaultDomainSuffix" configurado — sem essa convenção não há como
            // distinguir "host é o domínio padrão de algum tenant" de "host é qualquer outra
            // coisa" (ver docstring de PlatformDomainOptions e o gap de infra/cloud no relatório).
            return null;
        }

        var expectedSuffix = "." + suffix.Trim().Trim('.').ToLowerInvariant();
        if (!normalizedHost.EndsWith(expectedSuffix, StringComparison.Ordinal))
        {
            return null;
        }

        var slug = normalizedHost[..^expectedSuffix.Length];
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var tenant = await _db.Tenants.AsNoTracking()
            .Where(t => t.DeletedAt == null && t.Slug == slug)
            .Select(t => new { t.Domain })
            .FirstOrDefaultAsync(cancellationToken);

        return tenant?.Domain is { Length: > 0 } customDomain && customDomain != normalizedHost
            ? customDomain
            : null;
    }
}
