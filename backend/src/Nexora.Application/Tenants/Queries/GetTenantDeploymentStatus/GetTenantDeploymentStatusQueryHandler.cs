using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Tenants.Support;
using Nexora.Contracts.Platform;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Queries.GetTenantDeploymentStatus;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — cenário Gherkin "Resposta de
/// criação foi perdida": reconstrói o checklist de provisionamento a partir de FATOS PERSISTIDOS
/// (tenant existe desde que chegamos aqui; loja e instalação vêm de consultas próprias; token
/// consumido/instalação registrada vêm de <see cref="EdgeInstallation"/>), sem depender de o
/// administrador lembrar o que já tinha visto na tela de provisionamento original.
/// </summary>
/// <remarks>
/// PARECIDO com <c>GetTenantOverviewQueryHandler.BuildDeploymentAsync</c> (US-152) — mesma
/// contagem de passos do roteiro de implantação (ver <see cref="OnboardingChecklistCalculator"/>,
/// helper extraído para não duplicar SEM TESTAR essa lógica) — mas este handler é um arquivo
/// próprio, NÃO uma edição daquele: a disciplina de isolamento desta tarefa (E-15, múltiplos
/// agentes no mesmo working tree) proíbe editar um handler de outra história em paralelo. O campo
/// que justifica um endpoint dedicado em vez de reaproveitar <c>overview</c> é
/// <see cref="TenantDeploymentInstallationResponse.CanReissueToken"/>, que o overview não expõe.
/// </remarks>
internal sealed class GetTenantDeploymentStatusQueryHandler
    : IRequestHandler<GetTenantDeploymentStatusQuery, Result<TenantDeploymentStatusResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetTenantDeploymentStatusQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TenantDeploymentStatusResponse>> Handle(
        GetTenantDeploymentStatusQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == request.TenantId && t.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            return Result<TenantDeploymentStatusResponse>.Failure(
                "Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        // RLS (ADR-004): onboarding_step/edge_installation exigem app.tenant_id fixado — tenant é a
        // única tabela sem RLS (raiz global), por isso a busca acima roda sem este SetTenantContextAsync.
        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        var steps = await _db.OnboardingSteps.AsNoTracking()
            .Where(s => s.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        var (completed, total, nextActionKey) = OnboardingChecklistCalculator.Calculate(steps);
        var nextAction = nextActionKey is null ? null : OnboardingStepKeyWireFormat.ToWireKey(nextActionKey.Value);

        var installations = await _db.EdgeInstallations.AsNoTracking()
            .Where(i => i.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        // MVP (ver ADR-013/doc §14): hoje o provisionamento cria EXATAMENTE uma loja e uma
        // instalação por tenant — se um dia existir mais de uma (múltiplas lojas), a instalação
        // "relevante" para o fluxo de RECUPERAÇÃO é a que ainda não terminou o pareamento (é para
        // ela que "reemitir" faz sentido); na ausência de qualquer pendente, cai para a mais
        // recentemente criada (decisão documentada no relatório desta tarefa).
        var relevantInstallation = installations.FirstOrDefault(i => !i.IsInstalled)
            ?? installations.OrderByDescending(i => i.CreatedAt).FirstOrDefault();

        TenantDeploymentInstallationResponse? installationResponse = null;
        if (relevantInstallation is not null)
        {
            var status = !relevantInstallation.IsInstalled
                ? "PENDING"
                : relevantInstallation.Connectivity == SyncConnectivity.Offline ? "OFFLINE" : "ACTIVE";

            installationResponse = new TenantDeploymentInstallationResponse(
                relevantInstallation.Id, status, relevantInstallation.CanReissueToken);
        }

        var response = new TenantDeploymentStatusResponse(completed, total, installationResponse, nextAction);
        return Result<TenantDeploymentStatusResponse>.Success(response);
    }
}
