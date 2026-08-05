using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Onboarding.Commands.RecalculateOnboardingSteps;
using Nexora.Application.Onboarding.Support;
using Nexora.Contracts.Platform;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Onboarding.Queries.GetOnboardingStatus;

/// <summary>
/// [DECISÃO DE ARQUITETURA] Esta query dispara um comando (<see cref="RecalculateOnboardingStepsCommand"/>)
/// antes de ler — desvio deliberado da regra geral "query nunca escreve" (ver
/// <c>IQuery&lt;TResponse&gt;</c>). Justificativa (US-141 §3.1 "progresso visível para os dois
/// lados"): os passos derivados (BRANDING/MENU/TABLES/EDGE_INSTALL/PAYMENT_CONFIG) precisam refletir
/// o estado REAL do tenant a cada consulta, não só no instante em que algo mudou — e a escrita
/// disparada é idempotente (transição de status, nunca uma mutação de negócio) e passa pelo mesmo
/// pipeline (<c>TransactionBehavior</c>) de qualquer outro comando, então não há SaveChanges "por
/// fora" da infraestrutura padrão. Alternativa avaliada e descartada por ora: mover o recalculo para
/// um <c>BackgroundService</c> periódico — adiado porque criaria uma janela em que o dashboard
/// mostra progresso desatualizado logo após a ação do cliente (ex.: acabou de cadastrar produtos e o
/// checklist ainda mostraria MENU pendente).
/// </summary>
internal sealed class GetOnboardingStatusQueryHandler : IRequestHandler<GetOnboardingStatusQuery, Result<OnboardingStatusResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ISender _sender;

    public GetOnboardingStatusQueryHandler(IApplicationDbContext db, ISender sender)
    {
        _db = db;
        _sender = sender;
    }

    public async Task<Result<OnboardingStatusResponse>> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (tenant is null)
        {
            return Result<OnboardingStatusResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        await _sender.Send(new RecalculateOnboardingStepsCommand(request.TenantId), cancellationToken);

        var steps = await _db.OnboardingSteps
            .AsNoTracking()
            .Where(s => s.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);

        var productCount = await _db.Products
            .AsNoTracking()
            .CountAsync(p => p.TenantId == request.TenantId && p.DeletedAt == null, cancellationToken);

        var stepResponses = Enum.GetValues<OnboardingStepKey>()
            .Select(key =>
            {
                var step = steps.SingleOrDefault(s => s.Key == key);
                var status = step?.Status ?? OnboardingStepStatus.Pending;

                OnboardingStepProgressResponse? progress = key == OnboardingStepKey.Menu
                    ? new OnboardingStepProgressResponse(productCount, Expected: null)
                    : null;

                return new OnboardingStepResponse(
                    OnboardingStepKeyWireFormat.ToWireKey(key),
                    OnboardingStepKeyWireFormat.ToWireStatus(status),
                    progress);
            })
            .ToList();

        var elapsedBusinessDays = tenant.OnboardingStartedAt is { } startedAt
            ? OnboardingElapsedBusinessDays.Calculate(startedAt, DateTimeOffset.UtcNow)
            : (int?)null;

        return Result<OnboardingStatusResponse>.Success(
            new OnboardingStatusResponse(stepResponses, tenant.OnboardingStartedAt, elapsedBusinessDays));
    }
}
