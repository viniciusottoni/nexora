using Nexora.Application.Abstractions.Persistence;
using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.TenantDomains.Support;

/// <summary>
/// Leituras de <c>tenant_domain</c> que precisam atravessar todos os tenants sem conhecer de
/// antemão o dono (rotas <c>v1/platform/domains/*</c>, sem tenant no token — o ator é
/// PlatformAdmin). Mesmo padrão de <c>Nexora.Application.Installations.Support.PlatformInstallationLookup</c>:
/// como não existe hoje um papel de conexão com <c>BYPASSRLS</c>, a única forma de ler além do
/// próprio tenant é iterar <c>tenant</c> (raiz sem RLS) fixando <c>app.tenant_id</c> por vez até
/// achar a linha — aceitável na escala de um painel de plataforma (dezenas de tenants).
/// </summary>
internal static class TenantDomainPlatformLookup
{
    /// <summary>
    /// Localiza UM domínio por id sem conhecer o tenant dono — usado por
    /// <c>VerifyTenantDomainCommandHandler</c> (<c>POST /v1/platform/domains/{id}/verify</c>).
    /// Ao retornar, <c>app.tenant_id</c> já está fixado no tenant dono do domínio encontrado (o
    /// laço para exatamente nesse ponto) — o chamador pode seguir gravando no mesmo
    /// <see cref="IApplicationDbContext"/> sem precisar chamar <see cref="IApplicationDbContext.SetTenantContextAsync"/>
    /// de novo.
    /// </summary>
    public static async Task<TenantDomain?> FindByIdAsync(
        IApplicationDbContext db, Guid domainId, CancellationToken cancellationToken)
    {
        var tenantIds = await db.Tenants.AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            await db.SetTenantContextAsync(tenantId, cancellationToken);

            var domain = await db.TenantDomains
                .FirstOrDefaultAsync(d => d.Id == domainId && d.DeletedAt == null, cancellationToken);

            if (domain is not null)
            {
                return domain;
            }
        }

        return null;
    }

    /// <summary>
    /// Verdadeiro se QUALQUER tenant (não só o alvo do cadastro) já tem <paramref name="normalizedDomain"/>
    /// registrado — usado por <c>RegisterTenantDomainCommandHandler</c> para devolver 422 amigável
    /// antes de deixar o índice único global (<c>uq_tenant_domain_domain</c>, RN-015) estourar como
    /// 500. Deixa <c>app.tenant_id</c> no último tenant varrido quando não encontra nada; o
    /// chamador precisa fixar o contexto certo explicitamente antes de gravar (não reaproveitar o
    /// que sobrou daqui).
    /// </summary>
    public static async Task<bool> ExistsAsync(
        IApplicationDbContext db, string normalizedDomain, CancellationToken cancellationToken)
    {
        var tenantIds = await db.Tenants.AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            await db.SetTenantContextAsync(tenantId, cancellationToken);

            var exists = await db.TenantDomains.AsNoTracking()
                .AnyAsync(d => d.Domain == normalizedDomain && d.DeletedAt == null, cancellationToken);

            if (exists)
            {
                return true;
            }
        }

        return false;
    }
}
