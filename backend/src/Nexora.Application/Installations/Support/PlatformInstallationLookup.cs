using Nexora.Application.Abstractions.Persistence;
using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Installations.Support;

/// <summary>
/// Localiza UMA instalação edge por id sem o chamador precisar conhecer previamente o tenant dono
/// (US-140 §7 — rotas <c>GET /v1/platform/installations/{id}/...</c> só recebem o id da
/// instalação). Não existe hoje um papel de conexão com <c>BYPASSRLS</c> ligado à aplicação (só
/// <c>platform_admin</c> no banco, reservado — Docs/Domain/10 §1); a leitura entre tenants segue o
/// MESMO padrão de <c>ListPlatformInstallationsQueryHandler</c>/<c>InstallationHealthEvaluationWorker</c>:
/// itera <c>tenant</c> (sem RLS) fixando <c>app.tenant_id</c> por vez até achar a instalação.
/// Aceitável na escala de um painel de administração de plataforma (dezenas de tenants); se o
/// parque crescer a ponto de o scan pesar, o próximo passo é cache id→tenant ou o papel
/// <c>platform_admin</c> passar a ser usado nesta única leitura.
/// </summary>
internal static class PlatformInstallationLookup
{
    public static async Task<EdgeInstallation?> FindAsync(
        IApplicationDbContext db, Guid installationId, CancellationToken cancellationToken)
    {
        var tenantIds = await db.Tenants.AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            await db.SetTenantContextAsync(tenantId, cancellationToken);

            var installation = await db.EdgeInstallations.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == installationId, cancellationToken);

            if (installation is not null)
            {
                return installation;
            }
        }

        return null;
    }
}
