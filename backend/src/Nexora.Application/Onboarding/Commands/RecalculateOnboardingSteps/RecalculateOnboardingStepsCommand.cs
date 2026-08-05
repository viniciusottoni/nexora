using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Onboarding.Commands.RecalculateOnboardingSteps;

/// <summary>
/// Reavalia os sinais de estado real do tenant (US-141 §3.1) e atualiza os passos DERIVADOS do
/// roteiro de implantação (<c>BRANDING</c>, <c>MENU</c>, <c>TABLES</c>, <c>EDGE_INSTALL</c>,
/// <c>PAYMENT_CONFIG</c>) — idempotente: chamar de novo sem nenhuma mudança de estado real não
/// produz nenhuma escrita adicional, e um passo já <c>DONE</c> nunca regride. <c>TENANT_CREATED</c>
/// já nasce concluído (<c>OnboardingStep.SeedAll</c>); <c>TRAINING</c>/<c>PILOT</c>/<c>ACTIVATION</c>
/// não têm sinal automático (sem "ficha técnica pronta" ou "piloto executado" no modelo de dados) —
/// ficam só com <c>CompleteOnboardingStepCommand</c> (autoatendimento manual) e
/// <c>ActivateTenantCommand</c> (a própria ativação), respectivamente.
/// </summary>
public sealed record RecalculateOnboardingStepsCommand(Guid TenantId) : ICommand;
