using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-141 (Provisionamento autoatendido) — regras puras de <see cref="OnboardingStep"/>: seed dos
/// nove passos, e as transições <c>Start</c>/<c>Complete</c>. Sem banco (RLS/DbContext ficam em
/// <c>Nexora.IntegrationTests.OnboardingIntegrationTests</c>).
/// </summary>
public sealed class OnboardingStepTests
{
    [Fact]
    public void SeedAll_Cria_Os_Nove_Passos_Com_TenantCreated_Ja_Concluido()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var steps = OnboardingStep.SeedAll(tenantId, now);

        steps.Should().HaveCount(9);
        steps.Should().OnlyContain(s => s.TenantId == tenantId);
        steps.Select(s => s.Key).Should().BeEquivalentTo(Enum.GetValues<OnboardingStepKey>());

        var tenantCreated = steps.Single(s => s.Key == OnboardingStepKey.TenantCreated);
        tenantCreated.Status.Should().Be(OnboardingStepStatus.Done);
        tenantCreated.CompletedAt.Should().Be(now);

        steps.Where(s => s.Key != OnboardingStepKey.TenantCreated).Should().OnlyContain(
            s => s.Status == OnboardingStepStatus.Pending && s.CompletedAt == null);
    }

    [Fact]
    public void Start_Move_De_Pending_Para_InProgress()
    {
        var step = CreatePendingStep(OnboardingStepKey.Menu);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);

        step.Start(at);

        step.Status.Should().Be(OnboardingStepStatus.InProgress);
        step.UpdatedAt.Should().Be(at);
        step.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Start_Em_Passo_Ja_Concluido_Nao_Regride_Para_InProgress()
    {
        var step = CreatePendingStep(OnboardingStepKey.Menu);
        var completedAt = DateTimeOffset.UtcNow;
        step.Complete(completedAt, completedBy: Guid.NewGuid());

        step.Start(completedAt.AddMinutes(10));

        step.Status.Should().Be(OnboardingStepStatus.Done, "um passo concluído nunca regride (US-141 §3.1, recálculo idempotente)");
        step.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void Complete_Move_De_Pending_Direto_Para_Done()
    {
        var step = CreatePendingStep(OnboardingStepKey.Tables);
        var at = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();

        step.Complete(at, actor);

        step.Status.Should().Be(OnboardingStepStatus.Done);
        step.CompletedAt.Should().Be(at);
        step.CompletedBy.Should().Be(actor);
    }

    [Fact]
    public void Complete_Aceita_CompletedBy_Nulo_Para_Conclusao_Automatica_Por_Sinal_Derivado()
    {
        var step = CreatePendingStep(OnboardingStepKey.Branding);
        var at = DateTimeOffset.UtcNow;

        step.Complete(at, completedBy: null);

        step.Status.Should().Be(OnboardingStepStatus.Done);
        step.CompletedBy.Should().BeNull();
    }

    private static OnboardingStep CreatePendingStep(OnboardingStepKey key) =>
        OnboardingStep.SeedAll(Guid.NewGuid(), DateTimeOffset.UtcNow).Single(s => s.Key == key);
}
