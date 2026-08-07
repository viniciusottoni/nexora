using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Application.Installations.Support;
using Nexora.Application.Tenants.Support;
using Nexora.Contracts.Platform;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Queries.GetTenantOverview;

/// <summary>
/// US-152 "Visão 360 e acesso aos módulos do estabelecimento" — agregado de leitura só sobre
/// entidades que já existem (<c>tenant</c>, <c>store</c>, <c>edge_installation</c>,
/// <c>app_user</c>/<c>user_role</c>/<c>role</c> para o dono, <c>owner_invite</c>,
/// <c>onboarding_step</c>). Nenhuma tabela nova, nenhum dado operacional do cliente (RN-015).
/// Resiliência de seção (US-152 §12): a ausência de um proprietário resolvido NUNCA derruba o
/// restante da resposta — vira <c>owner: null</c>.
/// </summary>
internal sealed class GetTenantOverviewQueryHandler : IRequestHandler<GetTenantOverviewQuery, Result<TenantOverviewResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPlatformLinksResolver _platformLinksResolver;

    public GetTenantOverviewQueryHandler(IApplicationDbContext db, IPlatformLinksResolver platformLinksResolver)
    {
        _db = db;
        _platformLinksResolver = platformLinksResolver;
    }

    public async Task<Result<TenantOverviewResponse>> Handle(GetTenantOverviewQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == request.TenantId && t.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            return Result<TenantOverviewResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        // RLS (ADR-004): store/edge_installation/app_user/user_role/role/owner_invite/
        // onboarding_step exigem app.tenant_id fixado antes de qualquer leitura — tenant é a única
        // tabela sem RLS (raiz global), por isso a busca acima roda sem este SetTenantContextAsync.
        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var owner = await BuildOwnerAsync(tenant, now, cancellationToken);

        var stores = await _db.Stores.AsNoTracking()
            .Where(s => s.TenantId == tenant.Id && s.DeletedAt == null)
            .Select(s => new TenantOverviewStoreResponse(s.Id, s.Name, s.Timezone))
            .ToListAsync(cancellationToken);

        var installations = await BuildInstallationsAsync(tenant.Id, now, cancellationToken);

        var deployment = await BuildDeploymentAsync(tenant.Id, cancellationToken);

        var links = _platformLinksResolver.ResolveTenantLinks(tenant.Slug, tenant.Domain);
        var linksResponse = new TenantOverviewLinksResponse(links.PublicMenu, links.Admin, null);

        var tenantResponse = new TenantOverviewTenantResponse(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Status.ToWireLabel(),
            tenant.Plan,
            tenant.TemplateCode,
            tenant.Domain,
            tenant.CreatedAt,
            tenant.UpdatedAt,
            tenant.StatusVersion,
            TenantStatusTransitions.AdminTargetsFrom(tenant.Status).Select(s => s.ToWireLabel()).ToList());

        var response = new TenantOverviewResponse(tenantResponse, owner, stores, installations, deployment, linksResponse);

        return Result<TenantOverviewResponse>.Success(response);
    }

    /// <summary>
    /// Dono: <c>user_role</c> (TenantId) → <c>role.code = 'OWNER'</c> → <c>app_user</c>, primeira
    /// linha encontrada. Sem dono resolvido, devolve <c>null</c> (resiliência de seção, US-152 §12)
    /// — nunca lança, nunca derruba as demais seções.
    /// </summary>
    private async Task<TenantOverviewOwnerResponse?> BuildOwnerAsync(Domain.Platform.Tenant tenant, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var ownerUser = await (
            from userRole in _db.UserRoles.AsNoTracking()
            join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            join user in _db.Users.AsNoTracking() on userRole.UserId equals user.Id
            where userRole.TenantId == tenant.Id && role.Code == "OWNER"
            select user
        ).FirstOrDefaultAsync(cancellationToken);

        if (ownerUser is null)
        {
            return null;
        }

        var invite = await _db.OwnerInvites.AsNoTracking()
            .Where(i => i.TenantId == tenant.Id && i.UserId == ownerUser.Id)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var inviteStatus = ResolveInviteStatus(invite, now);
        var email = ownerUser.Email ?? tenant.OwnerEmail ?? string.Empty;

        return new TenantOverviewOwnerResponse(ownerUser.Name, email, inviteStatus);
    }

    /// <summary>
    /// Sem convite: usuário já tem credencial sem nunca ter precisado de convite — fallback
    /// defensivo, tratado como <c>ACCEPTED</c>. Com convite: <c>ConsumedAt</c> preenchido →
    /// <c>ACCEPTED</c>; não consumido e expirado → <c>EXPIRED</c>; caso contrário → <c>PENDING</c>.
    /// </summary>
    private static string ResolveInviteStatus(OwnerInvite? invite, DateTimeOffset now)
    {
        if (invite is null)
        {
            return "ACCEPTED";
        }

        if (invite.ConsumedAt is not null)
        {
            return "ACCEPTED";
        }

        if (invite.ExpiresAt <= now)
        {
            return "EXPIRED";
        }

        return "PENDING";
    }

    /// <summary>
    /// <c>status</c> deriva de <see cref="EdgeInstallation.IsInstalled"/>/<see cref="EdgeInstallation.Connectivity"/>
    /// (não é coluna): <c>PENDING</c> sem instalação concluída, <c>OFFLINE</c> quando
    /// <see cref="SyncConnectivity.Offline"/>, <c>ACTIVE</c> caso contrário. <c>health</c> reusa
    /// <see cref="InstallationHealthClassifier"/> por instalação instalada; instalação
    /// <c>PENDING</c> é sempre <c>UNKNOWN</c> — nunca reportar DOWN por ausência de evidência
    /// (mesma regra de <c>TenantHealthClassifier</c>, US-151).
    /// </summary>
    private async Task<List<TenantOverviewInstallationResponse>> BuildInstallationsAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var installations = await _db.EdgeInstallations.AsNoTracking()
            .Where(i => i.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var result = new List<TenantOverviewInstallationResponse>(installations.Count);

        foreach (var installation in installations)
        {
            string status;
            string health;

            if (!installation.IsInstalled)
            {
                status = "PENDING";
                health = "UNKNOWN";
            }
            else
            {
                status = installation.Connectivity == SyncConnectivity.Offline ? "OFFLINE" : "ACTIVE";
                health = InstallationHealthClassifier.Classify(now, installation.LastSeenAt).ToWireLabel();
            }

            result.Add(new TenantOverviewInstallationResponse(installation.Id, installation.Label, status, health));
        }

        return result;
    }

    /// <summary>
    /// <c>total</c> é sempre <see cref="Enum.GetValues{TEnum}"/> de <see cref="OnboardingStepKey"/>
    /// (9) — nunca a contagem de linhas persistidas, protegendo tenants semeados antes de todas as
    /// nove chaves existirem. <c>nextAction</c> é a chave (wire) do primeiro passo, na ORDEM do
    /// enum, cujo status não é <see cref="OnboardingStepStatus.Done"/> (linha ausente conta como
    /// <see cref="OnboardingStepStatus.Pending"/>); nulo quando <c>completed == total</c>.
    /// </summary>
    private async Task<TenantOverviewDeploymentResponse> BuildDeploymentAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var steps = await _db.OnboardingSteps.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var statusByKey = steps.ToDictionary(s => s.Key, s => s.Status);

        var total = Enum.GetValues<OnboardingStepKey>().Length;
        var completed = steps.Count(s => s.Status == OnboardingStepStatus.Done);

        string? nextAction = null;

        foreach (var key in Enum.GetValues<OnboardingStepKey>())
        {
            var status = statusByKey.TryGetValue(key, out var persistedStatus) ? persistedStatus : OnboardingStepStatus.Pending;

            if (status != OnboardingStepStatus.Done)
            {
                nextAction = OnboardingStepKeyWireFormat.ToWireKey(key);
                break;
            }
        }

        return new TenantOverviewDeploymentResponse(completed, total, nextAction);
    }
}
