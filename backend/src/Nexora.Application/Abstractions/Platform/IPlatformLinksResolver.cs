namespace Nexora.Application.Abstractions.Platform;

/// <summary>
/// Porta de resolução de URLs públicas/administrativas de um tenant (US-152 §10, campo
/// <c>links</c> de <c>GET /v1/platform/tenants/{id}/overview</c>) — pura (sem I/O, sem banco),
/// separada em Application/Infrastructure só porque a configuração real
/// (<c>PlatformDomainOptions.DefaultDomainSuffix</c>) vive em Infrastructure (ADR-039, "Application
/// nunca referencia Infrastructure"). Mesmo domínio "padrão" (<c>"{slug}.{DefaultDomainSuffix}"</c>)
/// que <c>TenantDomainRedirectResolver</c> (US-143) já usa no caminho inverso (host → tenant) — aqui
/// o caminho é o direto (tenant → link).
/// </summary>
public interface IPlatformLinksResolver
{
    /// <summary>
    /// Resolve os links do tenant a partir do domínio próprio verificado
    /// (<paramref name="customDomain"/>, quando houver) ou do domínio padrão da plataforma. Campos
    /// vêm <c>null</c> quando não há domínio resolvível para o ambiente corrente (US-152 §15
    /// "[PENDÊNCIA] definir URLs por ambiente") — nunca lança, nunca inventa um valor.
    /// </summary>
    PlatformTenantLinks ResolveTenantLinks(string slug, string? customDomain);
}

/// <summary>
/// Links administrativos resolvidos. <c>Health</c> do contrato de API nunca é preenchido por esta
/// porta (US-152 §15 mesma PENDÊNCIA) — o front usa navegação interna para o painel de Instalações
/// da US-140 em vez de um link externo.
/// </summary>
public sealed record PlatformTenantLinks(string? PublicMenu, string? Admin);
