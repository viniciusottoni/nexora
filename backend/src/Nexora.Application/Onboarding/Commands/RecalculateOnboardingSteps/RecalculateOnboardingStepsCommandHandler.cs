using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Onboarding.Support;
using Nexora.Domain.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Onboarding.Commands.RecalculateOnboardingSteps;

internal sealed class RecalculateOnboardingStepsCommandHandler : IRequestHandler<RecalculateOnboardingStepsCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public RecalculateOnboardingStepsCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(RecalculateOnboardingStepsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId;

        var steps = await _db.OnboardingSteps
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        // Tenant provisionado antes da US-141 (checklist nunca semeado) — nada a recalcular; não é
        // um erro (GetOnboardingStatusQueryHandler devolveria uma lista vazia de passos, aceitável
        // para dado histórico).
        if (steps.Count == 0)
        {
            return Result.Success();
        }

        var now = DateTimeOffset.UtcNow;

        var productCount = await _db.Products
            .CountAsync(p => p.TenantId == tenantId && p.DeletedAt == null, cancellationToken);
        var tableCount = await _db.DiningTables
            .CountAsync(t => t.TenantId == tenantId && t.DeletedAt == null, cancellationToken);
        var edgeInstalled = await _db.EdgeInstallations
            .AnyAsync(e => e.TenantId == tenantId && e.InstalledAt != null, cancellationToken);
        var tenantConfig = await _db.TenantConfigs
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);

        // BRANDING/PAYMENT_CONFIG: sinal binário (configurado ou não) — vai direto a DONE.
        ApplyBinarySignal(steps, OnboardingStepKey.Branding, OnboardingStepSignals.HasNonDefaultJson(tenantConfig?.Branding), now);
        ApplyBinarySignal(steps, OnboardingStepKey.Tables, tableCount > 0, now);
        ApplyBinarySignal(steps, OnboardingStepKey.EdgeInstall, edgeInstalled, now);
        ApplyBinarySignal(steps, OnboardingStepKey.PaymentConfig, OnboardingStepSignals.HasNonDefaultJson(tenantConfig?.Payments), now);

        // MENU: sinal PARCIAL — existe produto cadastrado, mas nenhuma fonte confiável indica
        // "cardápio completo" (nenhum campo de meta/alvo no modelo de dados hoje, ver
        // OnboardingStepProgressResponse.Expected). Consistente com o exemplo do contrato da US-141
        // §7 (44 produtos cadastrados e o passo ainda IN_PROGRESS, não DONE) — a conclusão do passo
        // MENU é sempre um ato explícito (CompleteOnboardingStepCommand), nunca automática.
        ApplyPartialSignal(steps, OnboardingStepKey.Menu, productCount > 0, now);

        return Result.Success();
    }

    private static void ApplyBinarySignal(List<OnboardingStep> steps, OnboardingStepKey key, bool signalPresent, DateTimeOffset now)
    {
        var step = steps.SingleOrDefault(s => s.Key == key);
        if (step is null || step.Status == OnboardingStepStatus.Done || !signalPresent)
        {
            return;
        }

        step.Complete(now, completedBy: null);
    }

    private static void ApplyPartialSignal(List<OnboardingStep> steps, OnboardingStepKey key, bool signalPresent, DateTimeOffset now)
    {
        var step = steps.SingleOrDefault(s => s.Key == key);
        if (step is null || step.Status == OnboardingStepStatus.Done || !signalPresent)
        {
            return;
        }

        step.Start(now);
    }
}
