using Nexora.Application.Abstractions.Platform;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Platform;

/// <summary>Implementação de <see cref="IPlatformLinksResolver"/> — ver docstring da porta.</summary>
public sealed class PlatformLinksResolver : IPlatformLinksResolver
{
    private readonly PlatformDomainOptions _options;

    public PlatformLinksResolver(IOptions<PlatformDomainOptions> options)
    {
        _options = options.Value;
    }

    public PlatformTenantLinks ResolveTenantLinks(string slug, string? customDomain)
    {
        var host = ResolveHost(slug, customDomain);
        return host is null
            ? new PlatformTenantLinks(null, null)
            : new PlatformTenantLinks($"https://{host}", $"https://{host}/admin");
    }

    private string? ResolveHost(string slug, string? customDomain)
    {
        if (!string.IsNullOrWhiteSpace(customDomain))
        {
            return customDomain;
        }

        var suffix = _options.DefaultDomainSuffix;
        if (string.IsNullOrWhiteSpace(suffix))
        {
            // Sem "Platform:DefaultDomainSuffix" configurado — mesmo comportamento seguro por
            // padrão de TenantDomainRedirectResolver (ver docstring de PlatformDomainOptions).
            return null;
        }

        return $"{slug}.{suffix.Trim().Trim('.').ToLowerInvariant()}";
    }
}
