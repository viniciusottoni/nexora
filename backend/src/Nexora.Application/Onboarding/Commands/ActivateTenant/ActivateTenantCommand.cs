using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Onboarding.Commands.ActivateTenant;

/// <summary>
/// Ativação ao final do roteiro de implantação (US-141 §4, cenário "Validação antes da ativação") —
/// porta de <c>POST /v1/platform/tenants/{id}/activate</c>. Só sucede quando os oito passos
/// anteriores a <c>ACTIVATION</c> estão <c>DONE</c> (recalculados antes da checagem — ver
/// <c>ActivateTenantCommandHandler</c>); ao suceder, fecha a medição de tempo de implantação
/// (<see cref="Nexora.Domain.Platform.Tenant.CompleteOnboarding"/>, RN "meta ≤ 5 dias úteis") e
/// marca o próprio passo <c>ACTIVATION</c> como concluído.
/// </summary>
public sealed record ActivateTenantCommand(Guid TenantId) : ICommand;
