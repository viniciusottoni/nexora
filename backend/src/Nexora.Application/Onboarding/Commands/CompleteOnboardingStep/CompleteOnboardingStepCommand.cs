using Nexora.Application.Abstractions.Messaging;
using Nexora.Domain.Platform;

namespace Nexora.Application.Onboarding.Commands.CompleteOnboardingStep;

/// <summary>
/// Conclusão manual de um passo do roteiro (US-141 §3.1 "assistente de configuração inicial no
/// painel do cliente") — o caminho de autoatendimento para os passos sem sinal automático confiável
/// (<c>TRAINING</c>, <c>PILOT</c>) e um fallback explícito para qualquer outro passo derivado
/// (<c>BRANDING</c>/<c>MENU</c>/<c>TABLES</c>/<c>EDGE_INSTALL</c>/<c>PAYMENT_CONFIG</c>) quando o
/// cliente/Replay quer avançar antes do sinal automático "pegar" (ex.: cardápio considerado pronto
/// mesmo sem atingir a contagem esperada). <c>ACTIVATION</c> é bloqueada aqui de propósito — só
/// <c>ActivateTenantCommand</c> pode concluí-la, porque essa conclusão precisa estar amarrada a
/// <c>Tenant.CompleteOnboarding</c> na mesma operação (RN "medição de tempo de implantação").
/// </summary>
public sealed record CompleteOnboardingStepCommand(Guid TenantId, OnboardingStepKey Key, Guid? CompletedBy) : ICommand;
